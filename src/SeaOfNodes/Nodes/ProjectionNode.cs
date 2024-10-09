﻿
namespace SeaOfNodes.Nodes
{
    public class ProjectionNode : Node
    {
        public ProjectionNode(int nodeId, IMultiNode node, int index)
         : base(nodeId, (Node)node)
        {
            this.Index = index;
        }

        public int Index { get; }

        protected override string Name => "Proj";

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