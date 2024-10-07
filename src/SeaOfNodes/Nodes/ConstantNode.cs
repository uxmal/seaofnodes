using Reko.Core.Expressions;
using System.Timers;

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

        protected override Node? Simplify()
        {
            return null;
        }
    }
}