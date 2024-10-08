using Reko.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaOfNodes.Nodes
{
    public class DefNode : Node
    {
        public DefNode(int nodeId, CFNode control, Storage stg) 
            : base(nodeId, control)
        {
            this.Storage = stg;
        }

        public Storage Storage { get; }

        protected override string Name => $"def_{Storage}";

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
