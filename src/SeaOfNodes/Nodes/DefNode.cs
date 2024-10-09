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

        public override T Accept<T>(INodeVisitor<T> visitor)
        {
            return visitor.VisitDefNode(this);
        }

        public override T Accept<T, C>(INodeVisitor<T, C> visitor, C ctx)
        {
            return visitor.VisitDefNode(this, ctx);
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
