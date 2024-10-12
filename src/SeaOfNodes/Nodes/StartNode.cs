using Reko.Core;
using SeaOfNodes.Analyses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaOfNodes.Nodes
{
    public class StartNode : CFNode
    {
        public StartNode(int nodeId, Procedure proc)
            : base(nodeId)
        {
            this.Procedure = proc;
        }

        protected override string Name => "Start";

        public Procedure Procedure { get; }

        public override T Accept<T>(INodeVisitor<T> visitor)
        {
            return visitor.VisitStartNode(this);
        }

        public override T Accept<T, C>(INodeVisitor<T, C> visitor, C ctx)
        {
            return visitor.VisitStartNode(this, ctx);
        }
        protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
        {
            writer.Write(Name);
            return writer;
        }
    }
}
