using Reko.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace SeaOfNodes.Nodes
{
    /// <summary>
    /// Represents a basic block from the original code. It has an input node
    /// for each block that precedes it, and 
    /// a single value which can be used to base control on.
    /// </summary>
    public class BlockNode : CFNode
    {
        public BlockNode(int nodeId, Block block) : base(nodeId)
        {
            this.Block = block;
        }

        public Block Block { get; }

        protected override string Name
        {
            get
            {
                if (this.Block == Block.Procedure.EntryBlock)
                    return "<Entry>";
                if (this.Block == Block.Procedure.ExitBlock)
                    return "<Exit>";
                return Block.Id;
            }
        }

        public override T Accept<T>(INodeVisitor<T> visitor)
        {
            return visitor.VisitBlockNode(this);
        }

        public override T Accept<T, C>(INodeVisitor<T, C> visitor, C ctx)
        {
            return visitor.VisitBlockNode(this, ctx);
        }

        protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
        {
            writer.Write(Block.Id);
            return writer;
        }

        protected override Node? Simplify()
        {
            return null;
        }
    }
}
