
using System.Xml.Linq;

namespace SeaOfNodes.Nodes;

public class CFProjectionNode : CFNode
{
    private readonly string name;

    public CFProjectionNode(int nodeId, IMultiNode node, int index, string name)
        : base(nodeId, (Node)node)
    {
        this.Index = index;
        this.name = name;
    }

    public int Index { get; }

    protected override string Name => $"{InNodes[0]?.NodeId ?? 0}.{name}";

    public override T Accept<T>(INodeVisitor<T> visitor)
    {
        return visitor.VisitCFProjectionNode(this);
    }

    public override T Accept<T, C>(INodeVisitor<T, C> visitor, C context)
    {
        return visitor.VisitCFProjectionNode(this, context);
    }

    protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
    {
        throw new NotImplementedException();
    }
}