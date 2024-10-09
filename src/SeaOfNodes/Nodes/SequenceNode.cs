
namespace SeaOfNodes.Nodes;

public class SequenceNode : Node
{
    public SequenceNode(int nodeId, params Node?[] elements)
        : base(nodeId, null, elements)
    {
    }

    protected override string Name => "SEQ";

    protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
    {
        throw new NotImplementedException();
    }

    protected override Node? Simplify()
    {
        throw new NotImplementedException();
    }
}