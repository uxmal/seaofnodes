using Reko.Core.Types;

namespace SeaOfNodes.Nodes;

public class StoreNode : Node, IMultiNode
{
    public StoreNode(int nodeId, DataType dt, Node memoryId, Node eaNode, Node srcNode)
        : base(nodeId, memoryId, eaNode, srcNode)
    {
        this.DataType = dt;
    }

    public DataType DataType { get; }

    protected override string Name => "Store";

    public override T Accept<T>(INodeVisitor<T> visitor)
    {
        return visitor.VisitStoreNode(this);
    }

    public override T Accept<T, C>(INodeVisitor<T, C> visitor, C context)
    {
        return visitor.VisitStoreNode(this, context);
    }

    protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
    {
        throw new NotImplementedException();
    }
}