namespace SeaOfNodes.Nodes
{
    public interface INodeVisitor<TResult>
    {
        TResult VisitBinaryNode(BinaryNode bin);
        TResult VisitBlockNode(BlockNode block);
        TResult VisitBranchNode(BranchNode branch);
        TResult VisitCallNode(CallNode call);
        TResult VisitCFProjectionNode(CFProjectionNode projection);
        TResult VisitConditionOf(ConditionOfNode cond);
        TResult VisitConstantNode(ConstantNode c);
        TResult VisitDefNode(DefNode def);
        TResult VisitMemoryAccessNode(MemoryAccessNode access);
        TResult VisitPhiNode(PhiNode phi);
        TResult VisitProcedureConstantNode(ProcedureConstantNode proc);
        TResult VisitProjectionNode(ProjectionNode projection);
        TResult VisitReturnNode(ReturnNode ret);
        TResult VisitSequenceNode(SequenceNode sequence);
        TResult VisitSliceNode(SliceNode slice);
        TResult VisitStartNode(StartNode start);
        TResult VisitStopNode(StopNode stop);
        TResult VisitStoreNode(StoreNode store);
        TResult VisitUnaryNode(UnaryNode unary);
        TResult VisitUseNode(UseNode use);
    }

    public interface INodeVisitor<TResult, TContext>
    {
        TResult VisitBinaryNode(BinaryNode bin, TContext ctx);
        TResult VisitBlockNode(BlockNode block, TContext ctx);
        TResult VisitBranchNode(BranchNode branch, TContext ctx);
        TResult VisitCallNode(CallNode call, TContext ctx);
        TResult VisitCFProjectionNode(CFProjectionNode projection, TContext ctx);
        TResult VisitConditionOf(ConditionOfNode cond, TContext ctx);
        TResult VisitConstantNode(ConstantNode c, TContext ctx);
        TResult VisitDefNode(DefNode def, TContext ctx);
        TResult VisitMemoryAccessNode(MemoryAccessNode access, TContext ctx);
        TResult VisitPhiNode(PhiNode phi, TContext ctx);
        TResult VisitProcedureConstantNode(ProcedureConstantNode proc, TContext ctx);
        TResult VisitProjectionNode(ProjectionNode projection, TContext ctx);
        TResult VisitReturnNode(ReturnNode ret, TContext ctx);
        TResult VisitSequenceNode(SequenceNode sequence, TContext ctx);
        TResult VisitSliceNode(SliceNode slice, TContext ctx);
        TResult VisitStartNode(StartNode start, TContext ctx);
        TResult VisitStopNode(StopNode stop, TContext ctx);
        TResult VisitStoreNode(StoreNode store, TContext ctx);
        TResult VisitUnaryNode(UnaryNode unary, TContext ctx);
        TResult VisitUseNode(UseNode use, TContext ctx);
    }
}