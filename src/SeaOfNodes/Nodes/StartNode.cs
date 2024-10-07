using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaOfNodes.Nodes
{
    public class StartNode : CFNode
    {
        public StartNode(int nodeId)
            : base(nodeId)
        {
        }

        protected override string Name => "Start";

        protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
        {
            writer.Write(Name);
            return writer;
        }

        protected override Node? Simplify()
        {
            return null;
        }
    }
}
