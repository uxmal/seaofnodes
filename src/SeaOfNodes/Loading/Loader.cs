using Reko.Core;
using Reko.Core.Code;
using Reko.Core.Collections;
using Reko.Core.Expressions;
using Reko.Core.Graphs;
using SeaOfNodes.Nodes;
using System.Diagnostics;

namespace SeaOfNodes.Loading;

/// <summary>
/// Loads a Reko <see cref="Procedure"/> into SeaOfNodes representation.
/// </summary>
public class Loader : InstructionVisitor<Node>, ExpressionVisitor<Node>
{
    private readonly NodeFactory factory;
    private readonly SsaGraphBuilder sgb;
    private Dictionary<Block, BlockNode> blockNodes;
    private Block blockCur;
    private CFNode? ctrlNode;   // The controlling control flow node.
    private Procedure proc;
    private BlockGraph cfg;

    public Loader(Procedure proc, NodeFactory factory)
    {
        this.proc = proc;
        this.cfg = proc.ControlGraph;
        this.factory = factory;
        this.blockNodes = [];
        this.sgb = new SsaGraphBuilder(proc.Architecture, blockNodes, factory);
        this.blockCur = default!;
    }

    public StopNode Load()
    {
        this.cfg = proc.ControlGraph;
        sgb.SealBlock(proc.ExitBlock);
        CreateBlockNodes(this.cfg);
        var wl = new WorkList<Block>();
        wl.Add(proc.EntryBlock);
        AddEdge(factory.StartNode, blockNodes[proc.EntryBlock]);
        var visited = new HashSet<Block>();
        while (wl.TryGetWorkItem(out var block))
        {
            if (!visited.Add(block))
                continue;
            this.blockCur = block;
            this.ctrlNode = blockNodes[block];
            sgb.EnterBlock(block);
            ProcessBlock(block);
            foreach (var succ in cfg.Successors(block))
            {
                if (ctrlNode is not null)
                {
                    AddEdge(ctrlNode, blockNodes[succ]);
                }
                wl.Add(succ);
            }
            sgb.SealBlock(block);
        }
        ProcessExitBlock(proc.ExitBlock);
        sgb.ProcessIncompletePhis();
        return factory.StopNode;
    }

    private void AddEdge(Node def, Node use)
    {
        def.AddUse(use);
        use.AddInput(def);
    }

    private void AddEdges(Node def, IEnumerable<Node> uses)
    {
        foreach (var use in uses)
        {
            AddEdge(def, use);
        }
    }

    private void AddEdges(IEnumerable<Node> defs, Node use)
    {
        foreach (var def in defs)
        {
            AddEdge(def, use);
        }
    }

    private void CreateBlockNodes(BlockGraph cfg)
    {
        foreach (var b in cfg.Blocks)
        {
            blockNodes.Add(b, factory.Block(b));
        }
    }

    private void ProcessBlock(Block block)
    {
        foreach (var stm in block.Statements)
        {
            var node = stm.Instruction.Accept(this);
        }
    }

    private void ProcessExitBlock(Block exitBlock)
    {
        AddEdge(blockNodes[exitBlock], factory.StopNode);
        sgb.UseDefinedStorages(exitBlock);
    }


    public Node VisitAddress(Address addr)
    {
        throw new NotImplementedException();
    }

    public Node VisitApplication(Application appl)
    {
        throw new NotImplementedException();
    }

    public Node VisitArrayAccess(ArrayAccess acc)
    {
        throw new NotImplementedException();
    }

    public Node VisitAssignment(Assignment ass)
    {
        Debug.Assert(ctrlNode is not null);
        var srcNode = ass.Src.Accept(this);
        sgb.WriteStorage(ass.Dst, blockCur, srcNode);
        return srcNode;
    }

    public Node VisitBinaryExpression(BinaryExpression binExp)
    {
        var leftNode = binExp.Left.Accept(this);
        var rightNode = binExp.Right.Accept(this);
        var binNode = factory.Binary(binExp.DataType, binExp.Operator, leftNode, rightNode);
        return binNode;
    }

    public Node VisitBranch(Branch branch)
    {
        Debug.Assert(ctrlNode is not null);
        var predicate = branch.Condition.Accept(this);
        var branchNode = factory.Branch(ctrlNode, predicate);
        var falseProj = factory.CFProject(branchNode, 0, "false");
        var trueProj = factory.CFProject(branchNode, 1, "true");
        AddEdge(falseProj, blockNodes[blockCur.Succ[0]]);
        AddEdge(trueProj, blockNodes[blockCur.Succ[1]]);
        ctrlNode = null;
        return branchNode;
    }

    public Node VisitCallInstruction(CallInstruction ci)
    {
        Debug.Assert(ctrlNode is not null);
        var fn = ci.Callee.Accept(this);
        var uses = ci.Uses.Select(u => factory.Use(u.Storage, ctrlNode, u.Expression.Accept(this))).ToArray();
        var call = factory.Call(ctrlNode, fn);
        AddEdges(uses, call);
        var defs = ci.Definitions.Select(d => sgb.WriteStorage(d.Storage, blockCur, factory.Def(call, d.Storage))).ToArray();
        ctrlNode = call;
        return call;
    }

    public Node VisitCast(Cast cast)
    {
        throw new NotImplementedException();
    }

    public Node VisitComment(CodeComment comment)
    {
        throw new NotImplementedException();
    }

    public Node VisitConditionalExpression(ConditionalExpression cond)
    {
        throw new NotImplementedException();
    }

    public Node VisitConditionOf(ConditionOf cof)
    {
        var expr = cof.Expression.Accept(this);
        var node = factory.ConditionOf(cof, expr);
        return node;
    }

    public Node VisitConstant(Constant c)
    {
        var node = factory.Constant(c);
        return node;
    }

    public Node VisitConversion(Conversion conversion)
    {
        throw new NotImplementedException();
    }

    public Node VisitDefInstruction(DefInstruction def)
    {
        throw new NotImplementedException();
    }

    public Node VisitDereference(Dereference deref)
    {
        throw new NotImplementedException();
    }

    public Node VisitFieldAccess(FieldAccess acc)
    {
        throw new NotImplementedException();
    }

    public Node VisitGotoInstruction(GotoInstruction gotoInstruction)
    {
        throw new NotImplementedException();
    }

    public Node VisitIdentifier(Identifier id)
    {
        var node = sgb.ReadStorage(id, blockCur);
        return node;
    }

    public Node VisitMemberPointerSelector(MemberPointerSelector mps)
    {
        throw new NotImplementedException();
    }

    public Node VisitMemoryAccess(MemoryAccess access)
    {
        var ea = access.EffectiveAddress.Accept(this);
        var memId = access.MemoryId.Accept(this);
        var mem = factory.Mem(access.DataType, memId, ea);
        return mem;
    }

    public Node VisitMkSequence(MkSequence seq)
    {
        throw new NotImplementedException();
    }

    public Node VisitOutArgument(OutArgument outArgument)
    {
        throw new NotImplementedException();
    }

    public Node VisitPhiAssignment(PhiAssignment phi)
    {
        throw new NotImplementedException();
    }

    public Node VisitPhiFunction(PhiFunction phi)
    {
        throw new NotImplementedException();
    }

    public Node VisitPointerAddition(PointerAddition pa)
    {
        throw new NotImplementedException();
    }

    public Node VisitProcedureConstant(ProcedureConstant pc)
    {
        return factory.ProcedureConstant(pc);
    }

    public Node VisitReturnInstruction(ReturnInstruction ret)
    {
        Debug.Assert(ctrlNode is not null);
        var retVal = ret.Expression?.Accept(this);
        var retNode = factory.Return(ctrlNode, retVal);
        ctrlNode.AddUse(retNode);
        AddEdge(retNode, blockNodes[proc.ExitBlock]);
        ctrlNode = null;
        return retNode;
    }

    public Node VisitScopeResolution(ScopeResolution scopeResolution)
    {
        throw new NotImplementedException();
    }

    public Node VisitSegmentedAddress(SegmentedPointer address)
    {
        throw new NotImplementedException();
    }

    public Node VisitSideEffect(SideEffect side)
    {
        throw new NotImplementedException();
    }

    public Node VisitSlice(Slice slice)
    {
        throw new NotImplementedException();
    }

    public Node VisitStore(Store store)
    {
        Debug.Assert(ctrlNode is not null);
        var srcNode = store.Src.Accept(this);
        var access = (MemoryAccess) store.Dst;
        var mid = sgb.ReadStorage(access.MemoryId, blockCur);
        var eaNode = access.EffectiveAddress.Accept(this);
        var storeNode = factory.Store(store.Dst.DataType, ctrlNode, eaNode, srcNode);
        ctrlNode = factory.CFProject(storeNode, 0, "ctrl");
        var newMem = factory.Project(storeNode, 1, "mem");
        sgb.WriteStorage(access.MemoryId, blockCur, newMem);
        return srcNode;
    }

    public Node VisitStringConstant(StringConstant str)
    {
        throw new NotImplementedException();
    }

    public Node VisitSwitchInstruction(SwitchInstruction si)
    {
        throw new NotImplementedException();
    }

    public Node VisitTestCondition(TestCondition tc)
    {
        throw new NotImplementedException();
    }

    public Node VisitUnaryExpression(UnaryExpression unary)
    {
        var expNode = unary.Expression.Accept(this);
        var unNode = factory.Unary(unary.DataType, unary.Operator, expNode);
        return unNode;
    }

    public Node VisitUseInstruction(UseInstruction use)
    {
        throw new NotImplementedException();
    }
}
