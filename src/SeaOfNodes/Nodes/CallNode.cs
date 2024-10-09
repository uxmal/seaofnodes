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

        public override T Accept<T>(INodeVisitor<T> visitor)
        {
            return visitor.VisitCallNode(this);
        }

        public override T Accept<T, C>(INodeVisitor<T, C> visitor, C ctx)
        {
            return visitor.VisitCallNode(this, ctx);
        }

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