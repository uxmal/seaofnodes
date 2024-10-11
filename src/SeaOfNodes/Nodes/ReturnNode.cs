
namespace SeaOfNodes.Nodes;

public class ReturnNode : CFNode
{
    private Node? retVal;

    public ReturnNode(int nodeId, CFNode blockNode, Node? retVal = null)
        : base(nodeId)
    {
        this.retVal = retVal;
        AddInput(blockNode);
        if (retVal is not null)
            AddInput(retVal);
    }

    protected override string Name => "return";

    public override T Accept<T>(INodeVisitor<T> visitor)
    {
        return visitor.VisitReturnNode(this);
    }

    public override T Accept<T, C>(INodeVisitor<T, C> visitor, C ctx)
    {
        return visitor.VisitReturnNode(this, ctx);
    }

    protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
    {
        writer.Write("return");
        if (retVal != null)
        {
            writer.Write(" ");
            retVal.Write(writer, visited);
        }
        return writer;
    }
}