using Reko.Core.Types;

namespace SeaOfNodes.Nodes
{
    public class SliceNode : Node
    {
        public DataType DataType { get; }

        protected override string Name => "slice";

        public int BitOffset { get; }

        public SliceNode(int nodeId, Node node, DataType dataType, int ioffset)
            : base(nodeId, null, node)
        {
            this.DataType = dataType;
            this.BitOffset = ioffset;
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