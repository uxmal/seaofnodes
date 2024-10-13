using Reko.Analysis;
using Reko.Core;
using Reko.Core.Code;
using Reko.Core.Expressions;
using Reko.Core.Lib;
using Reko.Core.Operators;
using Reko.Core.Types;
using SeaOfNodes.Nodes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace SeaOfNodes.Loading;


public class SsaGraphBuilder
{
    private readonly IProcessorArchitecture arch;
    private readonly NodeFactory factory;
    private readonly Dictionary<Block, BlockNode> blockNodes;
    private readonly Dictionary<Block, BlockState> states;
    private readonly HashSet<Block> sealedBlocks;
    private readonly Dictionary<StorageDomain, BitRange> definedRegisterStorages;
    private readonly Dictionary<RegisterStorage, uint> definedFlagGroups;
    private readonly List<(Worker, PhiNode)> incompletePhis;
    private MemoryStorage? memoryStg;

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
        this.definedRegisterStorages = [];
        this.definedFlagGroups = [];
        this.incompletePhis = [];
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
        if (memoryStg is not null)
        {
            var w = new MemoryWorker(this, memoryStg);
            var node = w.ReadStorage(exitBlock);
            var use = factory.Use(memoryStg, blockNodes[exitBlock], node);
            w.AddEdge(use, factory.StopNode);
        }
        foreach (var (domain, range) in definedRegisterStorages)
        {
            var stg = GetRegister(domain, range);
            var w = new RegisterWorker(this, domain, range);
            var node = w.ReadStorage(exitBlock);
            var use = factory.Use(stg, blockNodes[exitBlock], node);
            w.AddEdge(use, factory.StopNode);
        }

    }

    private Worker CreateWorker(SsaGraphBuilder ssaGraphBuilder, Storage stg)
    {
        return stg switch
        {
            RegisterStorage reg => new RegisterWorker(ssaGraphBuilder, reg),
            MemoryStorage mem => new MemoryWorker(ssaGraphBuilder, mem),
            TemporaryStorage tmp => new TemporaryWorker(ssaGraphBuilder, tmp),
            FlagGroupStorage grf => new FlagGroupWorker(ssaGraphBuilder, grf),
            _ => throw new NotImplementedException(stg.GetType().Name),
        };
    }

    public RegisterStorage GetRegister(StorageDomain domain, BitRange range)
    {
        var stg = this.arch.GetRegister(domain, range);
        Debug.Assert(stg is not null);
        return stg;
    }

    private static bool IsTemporary(StorageDomain domain)
    {
        return (uint)domain >= unchecked((uint)StorageDomain.Temporary);
    }

    public Node ReadStorage(Identifier id, Block block)
    {
        var w = CreateWorker(this, id.Storage);
        return w.ReadStorage(block);
    }

    public Node WriteStorage(Identifier id, Block block, Node node)
    {
        return WriteStorage(id.Storage, block, node);
    }

    public Node WriteStorage(Storage stg, Block block, Node node)
    {
        var w = CreateWorker(this, stg);
        return w.WriteStorage(block, node);
    }

    public void ProcessIncompletePhis()
    {
        while (incompletePhis.Count > 0)
        {
            var work = incompletePhis.ToArray();
            incompletePhis.Clear();
            foreach (var (w, phi) in work)
            {
                w.AddPhiOperands(phi);
            }
        }
    }

    private abstract class Worker
    {
        protected readonly SsaGraphBuilder builder;

        protected Worker(SsaGraphBuilder builder)
        {
            this.builder = builder;
        }

        public void AddEdge(Node def, Node use)
        {
            def.AddUse(use);
            use.AddInput(def);
        }

        protected abstract void DefineStorage();

        protected abstract Node CreateDefNode();

        public Node ReadStorage(Block block)
        {
            if (TryReadLocalStorage(block, out Node? local))
                return local;
            return ReadStorageRecursive(block);
        }

        protected Node ReadStorageRecursive(Block block)
        {
            var preds = block.Procedure.ControlGraph.Predecessors(block);
            if (preds.Count == 0)
            {
                // Live in parameter.
                var node = this.CreateDefNode();
                return node;
            }
            if (block.Pred.Any(p => !builder.sealedBlocks.Contains(p)))
            {
                // Incomplete CFG
                var phi = builder.factory.Phi(block);
                AddEdge(builder.blockNodes[block], phi);
                builder.incompletePhis.Add((this, phi));
                WriteStorage(block, phi);
                return phi;
            }
            Node? val;
            if (preds.Count == 1)
            {
                var pred = preds.First();
                // Optimize the common case of one predecessor: No phi needed
                if (!TryReadLocalStorage(pred, out val))
                    val = ReadStorageRecursive(pred);
            }
            else
            {
                // Break potential cycles with operandless phi
                var phi = builder.factory.Phi(block);
                AddEdge(builder.blockNodes[block], phi);
                WriteStorage(block, phi);
                val = AddPhiOperands(phi);
            }
            WriteStorage(block, val);
            return val;
        }

        public Node AddPhiOperands(PhiNode phi)
        {
            // Determine operands from predecessors
            foreach (var pred in phi.Block.Pred)
            {
                var var = ReadStorage(pred);
                phi.AddInput(var);
                var.AddUse(phi);
            }
            return TryRemoveTrivialPhi(phi);
        }

        private Node TryRemoveTrivialPhi(PhiNode phi)
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
                same = CreateDefNode();
            }
            var users = phi.OutNodes.Where(n => n != phi); // Remember all users except the phi itself
            ReplaceBy(users, phi, same); // Reroute all uses of phi to same and remove phi
                                         // Try to recursively remove all phi users, which might have become trivial
            foreach (var use in users)
            {
                if (use is PhiNode usingPhi)
                    TryRemoveTrivialPhi(usingPhi);
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

        public abstract bool TryReadLocalStorage(Block block, [MaybeNullWhen(false)] out Node local);

        public Node WriteStorage(Block block, Node node)
        {
            DefineStorage();
            return DoWriteStorage(block, node);
        }

        protected abstract Node DoWriteStorage(Block block, Node node);

    }

    private class RegisterWorker : Worker
    {
        private readonly StorageDomain domain;
        private readonly BitRange range;

        public RegisterWorker(SsaGraphBuilder builder, RegisterStorage reg)
            : base(builder)
        {
            this.domain = reg.Domain;
            this.range = reg.GetBitRange();
        }

        public RegisterWorker(SsaGraphBuilder builder, StorageDomain domain, BitRange range)
            : base(builder)
        {
            this.domain = domain;
            this.range = range;
        }

        protected override Node DoWriteStorage(Block block, Node node)
        {
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

        protected override void DefineStorage()
        {
            if (!builder.definedRegisterStorages.TryGetValue(domain, out var existingrange))
            {
                builder.definedRegisterStorages.Add(domain, range);
                return;
            }
            builder.definedRegisterStorages[domain] |= range;
        }

        protected override Node CreateDefNode()
        {
            var stg = builder.arch.GetRegister(domain, range);
            Debug.Assert(stg is not null);
            var node = builder.factory.Def(stg);
            return node;
        }

        public override bool TryReadLocalStorage(Block block, [MaybeNullWhen(false)] out Node local)
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
                    var w = new RegisterWorker(builder, this.domain, sliceAlias.Range);
                    var subNode = w.ReadStorageRecursive(block);
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
    }


    private class MemoryWorker : Worker
    {
        private readonly MemoryStorage mem;

        public MemoryWorker(SsaGraphBuilder builder, MemoryStorage mem)
            : base(builder)
        {
            this.mem = mem;
        }

        public override bool TryReadLocalStorage(Block block, [MaybeNullWhen(false)] out Node local)
        {
            local = builder.states[block].MemoryNode;
            return local is not null;
        }

        protected override Node CreateDefNode()
        {
            return builder.factory.StartNode;
        }

        protected override void DefineStorage()
        {
            builder.memoryStg = mem;
        }

        protected override Node DoWriteStorage(Block block, Node node)
        {
            builder.states[block].MemoryNode = node;
            return node;
        }
    }

    private class TemporaryWorker : Worker
    {
        private readonly TemporaryStorage tmp;

        public TemporaryWorker(SsaGraphBuilder builder, TemporaryStorage tmp)
            : base(builder)
        {
            this.tmp = tmp;
        }

        public override bool TryReadLocalStorage(Block block, [MaybeNullWhen(false)] out Node local)
        {
            return builder.states[block].TmpDefinitions.TryGetValue(tmp, out local);
        }

        protected override Node CreateDefNode()
        {
            return builder.factory.Def(tmp);
        }

        protected override void DefineStorage()
        {
        }

        protected override Node DoWriteStorage(Block block, Node node)
        {
            return builder.states[block].TmpDefinitions[tmp] = node;
        }
    }


    private class FlagGroupWorker : Worker
    {
        private readonly FlagGroupStorage grf;

        public FlagGroupWorker(SsaGraphBuilder builder, FlagGroupStorage grf)
            : base(builder)
        {
            this.grf = grf;
        }

        public override bool TryReadLocalStorage(Block block, [MaybeNullWhen(false)] out Node local)
        {
            var defs = builder.states[block].FlagDefinitions;
            if (!defs.TryGetValue(this.grf.FlagRegister, out var aliases))
            {
                local = default;
                return false;
            }
            var nodes = new List<Node>();
            var mask = grf.FlagGroupBits;
            for (int i = aliases.Count-1; i >= 0; --i)
            {
                var alias = aliases[i];
                if ((alias.Bits & mask) != 0)
                {
                    var node = MakeSlice(alias, alias.Bits & mask);
                    nodes.Add(node);
                    mask &= ~alias.Bits;
                }
            }
            if (mask != 0)
            {
                var flagGroup = builder.arch.GetFlagGroup(this.grf.FlagRegister, mask);
                Debug.Assert(flagGroup is not null);
                var fx = new FlagGroupWorker(this.builder, flagGroup);
                var node = fx.ReadStorageRecursive(block);
                nodes.Add(node);
            }
            if (nodes.Count == 1)
            {
                local = nodes[0];
                return true;
            }
            else
            {
                // Or'em
                local = nodes.Skip(1).Aggregate(
                    nodes[0],
                    (a, b) => builder.factory.Binary(
                        this.grf.FlagRegister.Width,
                        Operator.Or,
                        a,
                        b));
                return true;
            }
        }

        private Node MakeSlice(FlagGroupAlias alias, uint mask)
        {
            if (alias.Bits == mask)
                return alias.Node;  // Don't need to slice if the bits match exactly.
            var e = builder.factory.Binary(
                grf.Width,
                Operator.And,
                alias.Node,
                builder.factory.Constant(
                    Constant.Create(
                        grf.Width,
                        mask)));
            return e;
        }

        protected override Node CreateDefNode()
        {
            throw new NotImplementedException();
        }

        protected override void DefineStorage()
        {
            if (!builder.definedFlagGroups.TryGetValue(this.grf.FlagRegister, out uint bitsSet))
            {
                builder.definedFlagGroups.Add(this.grf.FlagRegister, this.grf.FlagGroupBits);
                return;
            }
            builder.definedFlagGroups[this.grf.FlagRegister] =
                bitsSet | this.grf.FlagGroupBits;
        }

        protected override Node DoWriteStorage(Block block, Node node)
        {
            var defs = builder.states[block].FlagDefinitions;
            if (!defs.TryGetValue(this.grf.FlagRegister, out var aliases))
            {
                aliases = [];
                defs.Add(this.grf.FlagRegister, aliases);
            }
            for (int i = aliases.Count - 1; i >= 0; --i)
            {
                var def = aliases[i];
                if ((~this.grf.FlagGroupBits & def.Bits) == 0)
                    aliases.RemoveAt(i);
            }
            aliases.Add(new(this.grf.FlagGroupBits, node));
            return node;
        }
    }


    private class BlockState
    {
        public BlockState()
        {
            this.Definitions = [];
            this.FlagDefinitions = [];
            this.TmpDefinitions = [];
        }

        public Dictionary<StorageDomain, List<StorageAlias>> Definitions { get; }
        public Dictionary<RegisterStorage, List<FlagGroupAlias>> FlagDefinitions { get; }
        public Dictionary<TemporaryStorage, Node> TmpDefinitions { get; }
        public Node? MemoryNode { get; set; }
    }

    [DebuggerDisplay("{Range} ({Node.Name})")]
    private readonly struct StorageAlias(BitRange range, Node node)
    {
        public BitRange Range { get; } = range;
        public Node Node { get; } = node;
    }

    [DebuggerDisplay("{Bits} ({Node.Name})")]
    private readonly struct FlagGroupAlias(uint bits, Node node)
    {
        public uint Bits { get; } = bits;

        public Node Node { get; } = node;
    }
}
