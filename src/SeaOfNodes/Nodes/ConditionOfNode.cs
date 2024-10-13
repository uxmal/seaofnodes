
namespace SeaOfNodes.Nodes;

public class ConditionOfNode : Node
{
    public ConditionOfNode(int nodeId, Node expr)
        : base(nodeId, null, expr)
    {
    }
    protected override string Name => "cond";

    public override T Accept<T>(INodeVisitor<T> visitor)
    {
        return visitor.VisitConditionOf(this);
    }

    public override T Accept<T, C>(INodeVisitor<T, C> visitor, C context)
    {
        return visitor.VisitConditionOf(this, context);
    }

    protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
    {
        throw new NotImplementedException();
    }
}