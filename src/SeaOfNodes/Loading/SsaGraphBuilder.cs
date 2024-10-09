using Reko.Core;
using Reko.Core.Expressions;
using Reko.Core.Lib;
using SeaOfNodes.Nodes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SeaOfNodes.Loading;

public class SsaGraphBuilder
{
    private readonly IProcessorArchitecture arch;
    private readonly NodeFactory factory;
    private readonly Dictionary<Block, BlockNode> blockNodes;
    private readonly Dictionary<Block, BlockState> states;
    private readonly HashSet<Block> sealedBlocks;
    private readonly HashSet<Storage> definedStorages;

    public SsaGraphBuilder(
        IProcessorArchitecture arch,
        Dictionary<Block, BlockNode> blockNodes,
        NodeFactory factory)
    {
        this.arch = arch;
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
        var range = stg.GetBitRange();
        for (int i = aliases.Count - 1; i >= 0; --i)
        {
            var def = aliases[i];
            if (range.Covers(def.Range))
                aliases.RemoveAt(i);
        }
        aliases.Add(new(range, node));
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

        var range = stg.GetBitRange();
        int ioffset = range.Lsb;
        int iNext = ioffset;
        var frags = new List<StorageAlias>();
        for (; ioffset < range.Msb;)
        {
            var next = FindNextFragment(ioffset, aliases);
            if (next.Node is null)
                break;
            iNext = next.Range.Lsb;
            if (iNext > ioffset)
            {
                var subStg = MakeSubstorage(stg, ioffset, iNext - ioffset);
                var subNode = ReadStorageRecursive(subStg, block);
                var a = new StorageAlias(subStg.GetBitRange(), subNode);
                frags.Add(a);
                ioffset = a.Range.Msb;
            }
            else
            {
                var sliceAlias = MakeSlice(stg, next.Range, next.Node);
                frags.Add(sliceAlias);
                ioffset = sliceAlias.Range.Msb;
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
    private StorageAlias FindNextFragment(int ioffset, List<StorageAlias> aliases)
    {
        BitRange rangeBest = default;
        Node? nodeBest = null;
        for (int i = 0; i < aliases.Count; ++i)
        {
            var a = aliases[i];
            if (a.Range.Lsb < ioffset)
                continue;
            if (nodeBest is null || a.Range.Lsb < rangeBest.Lsb)
            {
                rangeBest = a.Range;
                nodeBest = a.Node;
                continue;
            }
            if (a.Range.Lsb == rangeBest.Lsb)
            {
                if (a.Range.Extent < rangeBest.Extent)
                {
                    rangeBest = a.Range;
                    nodeBest = a.Node;
                }
            }
        }
        return new(rangeBest, nodeBest!);
    }

    private Storage MakeSubstorage(Storage stg, int ioffset, int v)
    {
        throw new NotImplementedException();
    }

    private StorageAlias MakeSlice(Storage stg,BitRange rangeSub, Node n)
    {
        var range = stg.GetBitRange().Intersect(rangeSub);
        if (rangeSub == range)
            return new(range, n);
        throw new NotImplementedException();
    }

    private Node MakeSequence(List<StorageAlias> frags)
    {
        throw new NotImplementedException();
    }

    private static bool FindExactFragment(Storage stg, List<StorageAlias> aliases, out Node local)
    {
        bool foundFragment = false;
        var range = stg.GetBitRange();
        for (int i = aliases.Count - 1; i >= 0; --i)
        {
            var alias = aliases[i];
            if (range == alias.Range &&
                !foundFragment)
            {
                // Exact match.
                local = alias.Node;
                return true;
            }
            if (range.Overlaps(alias.Range))
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

    private readonly struct StorageAlias(BitRange range, Node node)
    {
        public BitRange Range { get; } = range;
        public Node Node { get; } = node;

        public void Deconstruct(out BitRange range, out Node node)
        {
            range = this.Range;
            node = this.Node;
        }
    }
}
