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

    protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
    {
        writer.Write(Name);
        writer.Write(":");
        writer.Write(Storage.Name);
        return writer;
    }

    protected override Node? Simplify()
    {
        throw new NotImplementedException();
    }
}
