using Reko.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaOfNodes.Nodes
{
    /// <summary>
    /// A Phi function picks a value depending on control flow.
    /// It has as inputs the <see cref="BlockNode"/> in which it lives and
    /// a value from each control path.
    /// </summary>
    public class PhiNode : Node
    {
        public PhiNode(int nodeId, Block block, params Node[] inNodes)
         : base(nodeId, inNodes)
        { 
            Block = block;
        }

        public Block Block { get; }

        protected override string Name => $"phi_{NodeId}";

        public override T Accept<T>(INodeVisitor<T> visitor)
        {
            return visitor.VisitPhiNode(this);
        }

        public override T Accept<T, C>(INodeVisitor<T, C> visitor, C ctx)
        {
            return visitor.VisitPhiNode(this, ctx);
        }

        protected override Node? Simplify()
        {
            throw new NotImplementedException();
        }

        protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
        {
            writer.Write("Phi(");
            var sep = "";
            foreach (Node? @in in this.InNodes)
            {
                writer.Write(sep);
                if (@in is null)
                    writer.Write("____");
                else @in.Write(writer, visited);
                sep = ",";
            }
            writer.Write(")");
            return writer;
        }
    }
}
