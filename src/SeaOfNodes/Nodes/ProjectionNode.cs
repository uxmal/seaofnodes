
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

        public override T Accept<T>(INodeVisitor<T> visitor)
        {
            return visitor.VisitProjectionNode(this);
        }

        public override T Accept<T, C>(INodeVisitor<T, C> visitor, C ctx)
        {
            return visitor.VisitProjectionNode(this, ctx);
        }

        protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
        {
            throw new NotImplementedException();
        }
    }
}