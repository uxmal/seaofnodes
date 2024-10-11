
using Reko.Core.Types;
using System.Diagnostics;

namespace SeaOfNodes.Nodes
{
    public class MemoryAccessNode : Node
    {
        public MemoryAccessNode(int nodeId, DataType dataType, Node memId, Node effectiveAddress)
            : base(nodeId, memId, effectiveAddress)
        {
            this.DataType = dataType;
        }

        public DataType DataType { get; private set; }

        public Node MemoryId
        {
            get
            {
                var m = InNodes[0];
                Debug.Assert(m is not null);
                return m;
            }
        }

        protected override string Name => "Mem";

        public override T Accept<T>(INodeVisitor<T> visitor)
        {
            return visitor.VisitMemoryAccessNode(this);
        }

        public override T Accept<T, C>(INodeVisitor<T, C> visitor, C ctx)
        {
            return visitor.VisitMemoryAccessNode(this, ctx);
        }

        protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
        {
            throw new NotImplementedException();
        }
    }
}