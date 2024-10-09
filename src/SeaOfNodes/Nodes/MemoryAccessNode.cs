
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