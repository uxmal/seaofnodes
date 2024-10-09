using Reko.Core;
using Reko.Core.Expressions;
using SeaOfNodes.Nodes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SeaOfNodes.Loading;

public class SsaGraphBuilder
{
    private readonly NodeFactory factory;
    private readonly Dictionary<Block, BlockNode> blockNodes;
    private readonly Dictionary<Block, BlockState> states;
    private readonly HashSet<Block> sealedBlocks;
    private readonly HashSet<Storage> definedStorages;

    public SsaGraphBuilder(
        Dictionary<Block, BlockNode> blockNodes,
        NodeFactory factory)
    {
        this.factory = factory;
        this.blockNodes = blockNodes;
        this.states = [];
        this.sealedBlocks = [];
        this.definedStorages = [];
    }

    public void EnterBlock(Block block)
    {
        states.Add(block, new());
    }

    public void SealBlock(Block block)
    {
        sealedBlocks.Add(block);
    }

    public void UseDefinedStorages(Block exitBlock)
    {
        foreach (var stg in definedStorages)
        {
            var node = ReadStorage(stg, exitBlock);
            var use = factory.Use(stg, blockNodes[exitBlock], node);
            AddEdge(use, factory.StopNode);
        }
    }

    public Node WriteStorage(Identifier id, Block block, Node node)
    {
        return this.WriteStorage(id.Storage, block, node);
    }

    public Node WriteStorage(Storage stg, Block block, Node node)
    {
        this.definedStorages.Add(stg);
        var defs = states[block].Definitions;
        if (!defs.TryGetValue(stg.Domain, out var aliases))
        {
            aliases = [];
            defs.Add(stg.Domain, aliases);
        }
        for (int i = aliases.Count - 1; i >= 0; --i)
        {
            var def = aliases[i];
            if (stg.Covers(def.Storage))
                aliases.RemoveAt(i);
        }
        aliases.Add(new(stg, node));
        return node;
    }

    public Node ReadStorage(Storage stg, Block block)
    {
        if (TryReadLocalStorage(stg, block, out Node? local))
            return local;
        return ReadStorageRecursive(stg, block);
    }

    private Node ReadStorageRecursive(Storage stg, Block block)
    {
        var preds = block.Procedure.ControlGraph.Predecessors(block);
        if (preds.Count == 0)
        {
            // Live in parameter.
            var node = factory.Def(stg);
            return node;
        }
        if (block.Pred.Any(p => !sealedBlocks.Contains(p)))
        {
            // Incomplete CFG
            var phi = factory.Phi(block);
            AddEdge(blockNodes[block], phi);
            states[block].IncompletePhis[stg] = phi;
            WriteStorage(stg, block, phi);
            return phi;
        }
        Node? val;
        if (preds.Count == 1)
        {
            var pred = preds.First();
            // Optimize the common case of one predecessor: No phi needed
            if (!TryReadLocalStorage(stg, pred, out val))
                val = ReadStorageRecursive(stg, pred);
        }
        else
        {
            // Break potential cycles with operandless phi
            var phi = factory.Phi(block);
            AddEdge(blockNodes[block], phi);
            WriteStorage(stg, block, phi);
            val = AddPhiOperands(stg, phi);
        }
        WriteStorage(stg, block, val);
        return val;
    }

    private Node AddPhiOperands(Storage variable, PhiNode phi)
    {
        // Determine operands from predecessors
        foreach (var pred in phi.Block.Pred)
        {
            var var = ReadStorage(variable, pred);
            phi.AddInput(var);
            var.AddUse(phi);
        }
        return TryRemoveTrivialPhi(phi, variable);
    }

    private Node TryRemoveTrivialPhi(PhiNode phi, Storage stg)
    {
        Node? same = null;
        foreach (var op in phi.InNodes.Skip(1))
        {
            if (op == same || op == phi)
                continue;   // Unique value or self−reference
            if (same is not null)
                return phi; // The phi merges at least two values: not trivial
            same = op;
        }
        // The phi is unreachable or in the start block
        if (same is null)
            same = factory.Def(stg);
        var users = phi.OutNodes.Where(n => n != phi); // Remember all users except the phi itself
        ReplaceBy(users, phi, same); // Reroute all uses of phi to same and remove phi
                                     // Try to recursively remove all phi users, which might have become trivial
        foreach (var use in users)
        {
            if (use is PhiNode usingPhi)
                TryRemoveTrivialPhi(usingPhi, stg);
        }
        return same;
    }

    private void ReplaceBy(IEnumerable<Node> users, PhiNode phi, Node same)
    {
        foreach (var user in users)
        {
            for (int i = 0; i < user.InNodes.Count; ++i)
            {
                if (user.InNodes[i] == phi)
                    user.SetInput(i, same);
            }
        }
    }




    private bool TryReadLocalStorage(Storage stg, Block block, [MaybeNullWhen(false)] out Node local)
    {
        var defs = states[block].Definitions;
        if (!defs.TryGetValue(stg.Domain, out var aliases))
        {
            local = null;
            return false;
        }
        if (FindExactFragment(stg, aliases, out local))
            return true;

        ulong ioffset = stg.BitAddress;
        ulong iNext = ioffset;
        var frags = new List<StorageAlias>();
        for (; ioffset < stg.BitSize;)
        {
            var next = FindNextFragment(ioffset, aliases);
            if (next.Storage is null)
                break;
            iNext = next.Storage.BitAddress;
            if (iNext > ioffset)
            {
                var subStg = MakeSubstorage(stg, ioffset, iNext - ioffset);
                var subNode = ReadStorageRecursive(subStg, block);
                frags.Add(new(subStg, subNode));
                ioffset = subStg.BitAddress + subStg.BitSize;
            }
            else
            {
                var sliceStg = MakeSlice(stg, ioffset, iNext - ioffset);
                var sliceNode = factory.Slice(next.Node, sliceStg.DataType, ioffset);
                frags.Add(new(sliceStg, sliceNode));
                ioffset = sliceStg.BitAddress + sliceStg.BitSize;
            }
        }
        Debug.Assert(frags.Count >= 1);
        if (frags.Count == 1)
        {
            local = frags[0].Node;
        }
        else
        {
            frags.Reverse();
            local = MakeSequence(frags);
        }
        return true;
    }

    /// <summary>
    /// Find any alias that begins at <paramref name="ioffset"/> or later.
    /// </summary>
    /// <param name="ioffset"></param>
    /// <param name="aliases"></param>
    /// <returns></returns>
    private StorageAlias FindNextFragment(ulong ioffset, List<StorageAlias> aliases)
    {
        Storage? stgBest = null;
        Node? nodeBest = null;
        for (int i = 0; i < aliases.Count; ++i)
        {
            var a = aliases[i];
            if (a.Storage.BitAddress < ioffset)
                continue;
            if (a.Storage.BitAddress + a.Storage.BitSize >= ioffset)
                continue;
            if (stgBest is null || a.Storage.BitAddress < stgBest.BitAddress)
            {
                stgBest = a.Storage;
                nodeBest = a.Node;
                continue;
            }
            if (a.Storage.BitAddress == stgBest.BitAddress)
            {
                if (a.Storage.BitSize < stgBest.BitSize)
                {
                    stgBest = a.Storage;
                    nodeBest = a.Node;
                }
            }
        }
        return new(stgBest!, nodeBest!);
    }

    private Storage MakeSubstorage(Storage stg, ulong ioffset, ulong v)
    {
        throw new NotImplementedException();
    }

    private Storage MakeSlice(Storage stg, ulong ioffset, ulong v)
    {
        throw new NotImplementedException();
    }

    private Node MakeSequence(List<StorageAlias> frags)
    {
        throw new NotImplementedException();
    }

    private static bool FindExactFragment(Storage stg, List<StorageAlias> aliases, out Node local)
    {
        bool foundFragment = false;
        for (int i = aliases.Count - 1; i >= 0; --i)
        {
            var alias = aliases[i];
            if (alias.Storage == stg && !foundFragment)
            {
                // Exact match.
                local = alias.Node;
                return true;
            }
            if (alias.Storage.OverlapsWith(stg))
            {
                foundFragment = true;
            }
        }
        local = null!;
        return false;
    }

    private void AddEdge(Node def, Node use)
    {
        def.AddUse(use);
        use.AddInput(def);
    }



    private class BlockState
    {
        public BlockState()
        {
            this.Definitions = [];
            this.IncompletePhis = [];
        }
        public Dictionary<StorageDomain, List<StorageAlias>> Definitions { get; }
        public Dictionary<Storage, PhiNode> IncompletePhis { get; internal set; }
    }

    private readonly struct StorageAlias(Storage storage, Node node)
    {
        public Storage Storage { get; } = storage;
        public Node Node { get; } = node;

        public void Deconstruct(out Storage storage, out Node node)
        {
            storage = this.Storage;
            node = this.Node;
        }
    }
}
