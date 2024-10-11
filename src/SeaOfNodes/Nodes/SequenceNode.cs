
namespace SeaOfNodes.Nodes;

public class SequenceNode : Node
{
    public SequenceNode(int nodeId, params Node?[] elements)
        : base(nodeId, null, elements)
    {
    }

    protected override string Name => "SEQ";

    public override T Accept<T>(INodeVisitor<T> visitor)
    {
        return visitor.VisitSequenceNode(this);
    }

    public override T Accept<T, C>(INodeVisitor<T, C> visitor, C ctx)
    {
        return visitor.VisitSequenceNode(this, ctx);
    }

    protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
    {
        throw new NotImplementedException();
    }
}