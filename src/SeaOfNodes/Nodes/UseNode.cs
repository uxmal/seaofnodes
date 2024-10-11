using Reko.Core;

namespace SeaOfNodes.Nodes;

/// <summary>
/// Represents a Node that potentially returns a value to a caller via 
/// a specified <see cref="Storage"/>.
/// </summary>
public class UseNode : Node
{
    public UseNode(int nodeId, Storage stg, CFNode blockNode, Node node)
        : base(nodeId, blockNode, node)
    {
        this.Storage = stg;
    }

    public Storage Storage { get; }

    protected override string Name => $"use_{Storage}";

    public override T Accept<T>(INodeVisitor<T> visitor)
    {
        return visitor.VisitUseNode(this);
    }

    public override T Accept<T,C>(INodeVisitor<T,C> visitor, C ctx)
    {
        return visitor.VisitUseNode(this, ctx);
    }


    protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
    {
        writer.Write(Name);
        writer.Write(":");
        writer.Write(Storage.Name);
        return writer;
    }
}
