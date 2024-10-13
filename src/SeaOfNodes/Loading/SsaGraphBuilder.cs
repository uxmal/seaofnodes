using Reko.Core;
using Reko.Core.Expressions;
using Reko.Core.Lib;
using Reko.Core.Types;
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
    private readonly Dictionary<StorageDomain, BitRange> definedStorages;
    private readonly Dictionary<(Block, StorageDomain, BitRange), PhiNode> incompletePhis;
    private readonly Dictionary<StorageDomain, TemporaryStorage> temporaries;

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
        this.incompletePhis = [];
        this.temporaries = [];
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
        foreach (var (domain, range) in definedStorages)
        {
            var w = new Worker(this);
            var stg = w.GetStorage(domain, range);
            Debug.Assert(stg is not null);
            var node = w.ReadStorage(stg.Domain, stg.GetBitRange(), exitBlock);
            var use = factory.Use(stg, blockNodes[exitBlock], node);
            w.AddEdge(use, factory.StopNode);
        }
    }

    private static bool IsTemporary(StorageDomain domain)
    {
        return (uint)domain >= unchecked((uint)StorageDomain.Temporary);
    }

    public Node ReadStorage(Identifier id, Block block)
    {
        var w = new Worker(this);
        var stg = id.Storage;
        if (stg is TemporaryStorage tmp)
            this.temporaries.TryAdd(stg.Domain, tmp);
        return w.ReadStorage(stg.Domain, stg.GetBitRange(), block);
    }

    public Node WriteStorage(Identifier id, Block block, Node node)
    {
        var w = new Worker(this);
        return w.WriteStorage(id.Storage.Domain, id.Storage.GetBitRange(), block, node);
    }

    public Node WriteStorage(Storage stg, Block block, Node node)
    {
        var w = new Worker(this);
        return w.WriteStorage(stg.Domain, stg.GetBitRange(), block, node);
    }

    private class Worker
    {
        private readonly SsaGraphBuilder builder;

        public Worker(SsaGraphBuilder builder)
        {
            this.builder = builder;
        }

        public Storage GetStorage(StorageDomain domain, BitRange range)
        {
            Storage? stg;
            if (domain == StorageDomain.Memory)
                stg = MemoryStorage.Instance;
            else if (builder.temporaries.TryGetValue(domain, out var tmp))
                stg = tmp;
            else
                stg = builder.arch.GetRegister(domain, range);
            Debug.Assert(stg is not null);
            return stg;
        }



        public Node WriteStorage(StorageDomain domain, BitRange range, Block block, Node node)
        {
            DefineStorage(domain, range);
            var defs = builder.states[block].Definitions;
            if (!defs.TryGetValue(domain, out var aliases))
            {
                aliases = [];
                defs.Add(domain, aliases);
            }
            for (int i = aliases.Count - 1; i >= 0; --i)
            {
                var def = aliases[i];
                if (range.Covers(def.Range))
                    aliases.RemoveAt(i);
            }
            aliases.Add(new(range, node));
            return node;
        }

        private void DefineStorage(StorageDomain domain, BitRange range)
        {
            if (!builder.definedStorages.TryGetValue(domain, out var existingRange))
                builder.definedStorages.Add(domain, range);
            var newRange = range | existingRange;
            builder.definedStorages[domain] = newRange;
        }



        public Node ReadStorage(StorageDomain domain, BitRange range, Block block)
        {
            if (TryReadLocalStorage(domain, range, block, out Node? local))
                return local;
            return ReadStorageRecursive(domain, range, block);
        }

        private Node ReadStorageRecursive(StorageDomain domain, BitRange range, Block block)
        {
            var preds = block.Procedure.ControlGraph.Predecessors(block);
            if (preds.Count == 0)
            {
                // Live in parameter.
                if (domain == StorageDomain.Memory)
                    return builder.factory.StartNode;
                var stg = GetStorage(domain, range);
                var node = builder.factory.Def(stg);
                return node;
            }
            if (block.Pred.Any(p => !builder.sealedBlocks.Contains(p)))
            {
                // Incomplete CFG
                var phi = builder.factory.Phi(block);
                AddEdge(builder.blockNodes[block], phi);
                builder.incompletePhis[(block, domain, range)] = phi;
                WriteStorage(domain, range, block, phi);
                return phi;
            }
            Node? val;
            if (preds.Count == 1)
            {
                var pred = preds.First();
                // Optimize the common case of one predecessor: No phi needed
                if (!TryReadLocalStorage(domain, range, pred, out val))
                    val = ReadStorageRecursive(domain, range, pred);
            }
            else
            {
                // Break potential cycles with operandless phi
                var phi = builder.factory.Phi(block);
                AddEdge(builder.blockNodes[block], phi);
                WriteStorage(domain, range, block, phi);
                val = AddPhiOperands(domain, range, phi);
            }
            WriteStorage(domain, range, block, val);
            return val;
        }

        public Node AddPhiOperands(StorageDomain domain, BitRange range, PhiNode phi)
        {
            // Determine operands from predecessors
            foreach (var pred in phi.Block.Pred)
            {
                var var = ReadStorage(domain, range, pred);
                phi.AddInput(var);
                var.AddUse(phi);
            }
            return TryRemoveTrivialPhi(phi, domain, range);
        }

        private Node TryRemoveTrivialPhi(PhiNode phi, StorageDomain domain, BitRange range)
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
            {
                var stg = builder.arch.GetRegister(domain, range);
                Debug.Assert(stg is not null);
                same = builder.factory.Def(stg);
            }
            var users = phi.OutNodes.Where(n => n != phi); // Remember all users except the phi itself
            ReplaceBy(users, phi, same); // Reroute all uses of phi to same and remove phi
                                         // Try to recursively remove all phi users, which might have become trivial
            foreach (var use in users)
            {
                if (use is PhiNode usingPhi)
                    TryRemoveTrivialPhi(usingPhi, domain, range);
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

        private bool TryReadLocalStorage(StorageDomain domain, BitRange range, Block block, [MaybeNullWhen(false)] out Node local)
        {
            var defs = builder.states[block].Definitions;
            if (!defs.TryGetValue(domain, out var aliases))
            {
                local = null;
                return false;
            }
            if (FindExactFragment(range, aliases, out local))
                return true;

            int ioffset = range.Lsb;
            int iNext = ioffset;
            var frags = new List<Node>();
            for (; ioffset < range.Msb;)
            {
                var next = FindNextFragment(ioffset, aliases);
                if (next.Node is null)
                    break;
                iNext = next.Range.Lsb;
                if (iNext > ioffset)
                {
                    var sliceAlias = next;
                    var subNode = ReadStorageRecursive(domain, sliceAlias.Range, block);
                    var a = new StorageAlias(sliceAlias.Range, subNode);
                    frags.Add(a.Node);
                    ioffset = a.Range.Msb;
                }
                else
                {
                    frags.Add(next.Node);
                    ioffset = next.Range.Msb;
                }
            }
            Debug.Assert(frags.Count >= 1);
            if (frags.Count == 1)
            {
                local = frags[0];
            }
            else
            {
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
            StorageAlias aliasBest = default;
            for (int i = 0; i < aliases.Count; ++i)
            {
                var a = aliases[i];
                if (!a.Range.Contains(ioffset))
                    continue;
                if (aliasBest.Node is null || a.Range.Lsb < rangeBest.Lsb)
                {
                    rangeBest = new(ioffset, a.Range.Msb);
                    aliasBest = a;
                    continue;
                }
                if (a.Range.Lsb == rangeBest.Lsb)
                {
                    if (a.Range.Extent < rangeBest.Extent)
                    {
                        rangeBest = a.Range;
                        aliasBest = a;
                    }
                }
            }
            if (aliasBest.Node is null)
                return aliasBest;
            if (aliasBest.Range == rangeBest)
                return new(rangeBest, aliasBest.Node);
            var dt = PrimitiveType.CreateWord(rangeBest.Extent);
            var slicedNode = builder.factory.Slice(aliasBest.Node, dt, (uint)rangeBest.Msb);
            return new(rangeBest, slicedNode);
        }

        private Storage MakeSubstorage(Storage stg, int ioffset, int v)
        {
            throw new NotImplementedException();
        }

        private Node MakeSequence(List<Node> frags)
        {
            // Reko wants sequences is big-endian order, but 
            // we've collected the fragments in little-endian order.
            frags.Reverse();
            return builder.factory.Seq(frags.ToArray());
        }

        private static bool FindExactFragment(BitRange range, List<StorageAlias> aliases, out Node local)
        {
            bool foundFragment = false;
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

        public void AddEdge(Node def, Node use)
        {
            def.AddUse(use);
            use.AddInput(def);
        }
    }

    public void ProcessIncompletePhis()
    {
        while (incompletePhis.Count > 0)
        {
            var work = incompletePhis.ToArray();
            incompletePhis.Clear();
            foreach (var ((_, dom, z), phi) in work)
            {
                var w = new Worker(this);
                w.AddPhiOperands(dom, z, phi);
            }
        }
    }


    private class BlockState
    {
        public BlockState()
        {
            this.Definitions = [];
        }
        public Dictionary<StorageDomain, List<StorageAlias>> Definitions { get; }
    }

    [DebuggerDisplay("{Range} ({Node.Name})")]
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
