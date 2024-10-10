using Reko.Core.Operators;
using Reko.Core.Types;

namespace SeaOfNodes.Nodes
{
    public class BinaryNode : Node
    {

        public BinaryNode(int nodeId, DataType dataType, BinaryOperator @operator, Node leftNode, Node rightNode)
            : base(nodeId, null, leftNode, rightNode)
        {
            this.DataType = dataType;
            this.Operator = @operator;
            this.Left = leftNode;
            this.Right = rightNode;
        }

        public DataType DataType { get; }

        protected override string Name => Operator.ToString()!;

        public BinaryOperator Operator { get; }
        public Node Left { get; }
        public Node Right { get; }


        public override T Accept<T>(INodeVisitor<T> visitor)
        {
            return visitor.VisitBinaryNode(this);
        }

        public override T Accept<T, C>(INodeVisitor<T, C> visitor, C ctx)
        {
            return visitor.VisitBinaryNode(this, ctx);
        }

        protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
        {
            writer.Write('(');
            Left.Write(writer, visited);
            writer.Write(' ');
            writer.Write(this.Operator);
            writer.Write(' ');
            Right.Write(writer, visited).Write(')');
            return writer;
        }

        protected override Node? Simplify()
        {
            throw new NotImplementedException();
        }
    }
}