using Reko.Core.Operators;
using Reko.Core.Types;

namespace SeaOfNodes.Nodes
{
    public class UnaryNode : Node
    {
        public UnaryNode(int nodeId, DataType dataType, UnaryOperator @operator, Node? value, Node expNode)
            : base(nodeId, value, expNode)
        {
            this.DataType = dataType;
            this.Operator = @operator;
        }

        protected override string Name => Operator.ToString()!;

        public DataType DataType { get; }
        
        public UnaryOperator Operator { get; }

        public override T Accept<T>(INodeVisitor<T> visitor)
        {
            return visitor.VisitUnaryNode(this);
        }

        public override T Accept<T, C>(INodeVisitor<T, C> visitor, C ctx)
        {
            return visitor.VisitUnaryNode(this, ctx);
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