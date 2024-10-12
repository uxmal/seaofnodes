using Reko.Core;
using Reko.Core.Code;
using Reko.Core.Expressions;
using Reko.Core.Graphs;
using Reko.Core.Types;

namespace SeaOfNodes.UnitTests.Loading;

public class ProcedureBuilder : ExpressionEmitter
{
    private static readonly Block dummy = new Block(null!, null!, "@DUMMY@");

    private readonly Procedure proc;
    private readonly Dictionary<string, Block> labeledBlocks;
    private readonly List<(Block, string)> localFixups;
    private readonly List<(Statement, string)> externFixups;
    private Block? blockBranch;
    private Block? blockCur;
    private Address addr;

    public ProcedureBuilder(
        IProcessorArchitecture arch,
        string name, 
        Address addr)
    {
        this.proc = new Procedure(arch, name, addr, arch.CreateFrame());
        this.labeledBlocks = [];
        this.localFixups = [];
        this.externFixups = [];
        this.blockCur = null;
        this.addr = addr;
    }

    public Expression FramePointer => proc.Frame.FramePointer;

    public void Fixup(Dictionary<string, Procedure> symbols, CallGraph callgraph)
    {
        foreach (var (stm, fnName) in externFixups)
        {
            var call = (CallInstruction)stm.Instruction;
            var callee = new ProcedureConstant(
                proc.Architecture.PointerType,
                symbols[fnName]);
            var newCall = new CallInstruction(callee, call.CallSite);
            newCall.Uses.UnionWith(call.Uses);
            newCall.Definitions.UnionWith(call.Definitions);
            stm.Instruction = newCall;
        }
    }

    public Procedure ToProcedure()
    { 
        if (blockCur is not null)
        {
            proc.ControlGraph.AddEdge(blockCur, proc.ExitBlock);
        }
        ResolveFixups();
        return proc;
    }

    private void ResolveFixups()
    {
        foreach (var (block, label) in localFixups)
        {
            var stm = block.Statements.Last();
            switch (stm.Instruction)
            {
            case Branch branch:
                var target = this.labeledBlocks[label];
                stm.Instruction = new Branch(branch.Condition, target);
                proc.ControlGraph.AddEdge(block, target);
                break;
            default:
                // Must have been a goto.
                target = this.labeledBlocks[label];
                proc.ControlGraph.AddEdge(block, target);
                break;
            }
        }
    }

    public void Label(string? name = null)
    {
        name ??= $"l{addr.Offset:X8}";
        var blockNew = proc.AddBlock(addr, name);
        labeledBlocks.Add(name, blockNew);
        if (blockCur is not null)
        {
            proc.ControlGraph.AddEdge(blockCur, blockNew);
        }
        if (blockBranch is not null)
        {
            proc.ControlGraph.AddEdge(blockBranch, blockNew);
            blockBranch = null;
        }
        blockCur = blockNew;
    }

    private Statement Emit(Instruction instr)
    {
        var stm = EnsureBlock(null).Statements.Add(addr, instr);
        addr += 4;
        return stm;
    }

    private Block EnsureBlock(string? name)
    {
        if (blockCur is not null)
            return blockCur;
        name ??= $"l{addr}";
        var blockNew = proc.AddBlock(addr, name);
        if (blockBranch is not null)
        {
            proc.ControlGraph.AddEdge(blockBranch, blockNew);
            blockBranch = null;
        }
        if (proc.EntryBlock.Succ.Count == 0)
        {
            proc.ControlGraph.AddEdge(proc.EntryBlock, blockNew);
        }
        blockCur = blockNew;
        return blockNew;
    }

    public void Assign(Identifier dst, Expression src)
    {
        Emit(new Assignment(dst, src));
    }

    public void Assign(Identifier dst, long src)
    {
        var cSrc = Constant.Create(dst.DataType, src);
        Emit(new Assignment(dst, cSrc));
    }


    public void Assign(RegisterStorage dst, long value)
    {
        var id = proc.Frame.EnsureRegister(dst);
        Emit(new Assignment(id, Constant.Create(id.DataType, value)));
    }

    public void Branch(Expression predicate, string label)
    {
        this.blockBranch = EnsureBlock(null);
        localFixups.Add((blockBranch, label));
        Emit(new Branch(predicate, dummy));
        blockBranch = blockCur;
        blockCur = null;
    }

    public CallBuilder Call(Expression dst)
    {
        var site = new CallSite(0, 0);
        var call = new CallInstruction(dst, site);
        Emit(call);
        return new CallBuilder(call);
    }

    public CallBuilder Call(string fnName)
    {
        var block = EnsureBlock(null);
        var site = new CallSite(0, 0);
        var call = new CallInstruction(InvalidConstant.Create(proc.Architecture.PointerType), site); // Placeholder
        var stm = Emit(call);
        externFixups.Add((stm, fnName));
        return new CallBuilder(call);
    }

    public void Goto(string label)
    {
        var block = EnsureBlock(null);
        localFixups.Add((block, label));
        blockCur = null;
    }

    public void MStore(Expression effectiveAddress, Expression src)
    {
        var dst = this.Mem(src.DataType, effectiveAddress);
        Emit(new Store(dst, src));
    }

    public void Return()
    {
        Emit(new ReturnInstruction());
        proc.ControlGraph.AddEdge(EnsureBlock(null), proc.ExitBlock);
        blockCur = null;
    }

    public void Use(Storage stg)
    {
        var id = proc.Frame.EnsureIdentifier(stg);
        var eblock = proc.ExitBlock;
        eblock.Statements.Add(
            eblock.Address, 
            new UseInstruction(id));
    }

    public Identifier Reg(RegisterStorage reg)
    {
        var id = proc.Frame.EnsureRegister(reg);
        return id;
    }

    //$TODO: implement this in Reko
    public MemoryAccess Mem32(Expression baseAddr, long offset)
    {
        var cOffset = Constant.Create(
            PrimitiveType.Create(
                Domain.SignedInt,
                baseAddr.DataType.BitSize),
            offset);
        return Mem32(IAdd(baseAddr, cOffset));
    }

    public class CallBuilder
    {
        private CallInstruction call;

        public CallBuilder(CallInstruction call)
        {
            this.call = call;
        }

        public CallBuilder Def(Storage stg, Identifier id)
        {
            call.Definitions.Add(new CallBinding(stg, id));
            return this;
        }

        public CallBuilder Def(Identifier id)
        {
            call.Definitions.Add(new CallBinding(id.Storage, id));
            return this;
        }

        public CallBuilder Use(Storage stg, Expression e)
        {
            call.Uses.Add(new CallBinding(stg, e));
            return this;
        }

        public CallBuilder Use(Identifier id)
        {
            call.Uses.Add(new CallBinding(id.Storage, id));
            return this;
        }
    }


}