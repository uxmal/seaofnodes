
namespace SeaOfNodes.Nodes
{
    public class BranchNode : CFNode, IMultiNode
    {
        public BranchNode(int nodeId, CFNode ctrlNode, Node predicate) :
            base(nodeId, ctrlNode, predicate)
        {
        }

        protected override string Name => "branch";

        public CFNode CtrlNode => (BlockNode) InNodes[0]!;

        public Node Predicate => InNodes[1]!;

        public override T Accept<T>(INodeVisitor<T> visitor)
        {
            return visitor.VisitBranchNode(this);
        }

        public override T Accept<T, C>(INodeVisitor<T, C> visitor, C ctx)
        {
            return visitor.VisitBranchNode(this, ctx);
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