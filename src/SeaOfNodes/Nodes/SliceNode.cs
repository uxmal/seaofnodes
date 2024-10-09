using Reko.Core.Types;

namespace SeaOfNodes.Nodes
{
    public class SliceNode : Node
    {

        public SliceNode(int nodeId, Node node, DataType dataType, int ioffset)
            : base(nodeId, null, node)
        {
            this.DataType = dataType;
            this.BitOffset = ioffset;
        }
        public DataType DataType { get; }

        protected override string Name => "slice";

        public int BitOffset { get; }



        public override T Accept<T>(INodeVisitor<T> visitor)
        {
            return visitor.VisitSliceNode(this);
        }

        public override T Accept<T, C>(INodeVisitor<T, C> visitor, C ctx)
        {
            return visitor.VisitSliceNode(this, ctx);
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