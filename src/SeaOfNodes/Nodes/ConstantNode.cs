using Reko.Core.Expressions;

namespace SeaOfNodes.Nodes
{
    public class ConstantNode : Node
    {
        public ConstantNode(int nodeId, StartNode start, Constant value)
            : base(nodeId, start)
        {
            this.Value = value;
        }

        protected override string Name => $"#{Value}";

        protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
        {
            writer.Write(Value);
            return writer;
        }

        public Constant Value { get; }

        public override T Accept<T>(INodeVisitor<T> visitor)
        {
            return visitor.VisitConstantNode(this);
        }

        public override T Accept<T, C>(INodeVisitor<T, C> visitor, C ctx)
        {
            return visitor.VisitConstantNode(this, ctx);
        }
        protected override Node? Simplify()
        {
            return null;
        }
    }
}