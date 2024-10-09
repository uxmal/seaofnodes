using System.Runtime.Intrinsics.X86;

namespace SeaOfNodes.Nodes
{
    public class CallNode : CFNode
    {
        public CallNode(int nodeId, CFNode bn, Node fn)
            : base(nodeId, bn, fn)
        {
        }

        protected override string Name => "call";

        protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
        {
            throw new NotImplementedException();
        }

        protected override Node? Simplify()
        {
            throw new NotImplementedException();
        }
    }
}