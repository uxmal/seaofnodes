using Reko.Core.Expressions;

namespace SeaOfNodes.Nodes;

public class ProcedureConstantNode : Node
{
    public ProcedureConstantNode(int nodeId, StartNode startNode, ProcedureConstant pc)
        : base(nodeId, startNode)
    {
        this.Procedure = pc;
    }

    public ProcedureConstant Procedure { get; }

    protected override string Name => Procedure.Procedure.Name;

    public override T Accept<T>(INodeVisitor<T> visitor)
    {
        return visitor.VisitProcedureConstantNode(this);
    }

    public override T Accept<T, C>(INodeVisitor<T, C> visitor, C ctx)
    {
        return visitor.VisitProcedureConstantNode(this, ctx);
    }

    protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
    {
        throw new NotImplementedException();
    }
}
