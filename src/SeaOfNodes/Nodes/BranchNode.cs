
namespace SeaOfNodes.Nodes
{
    public class BranchNode : CFNode, IMultiNode
    {
        public BranchNode(int nodeId, BlockNode ctrlNode, Node predicate) :
            base(nodeId, ctrlNode, predicate)
        {
        }

        protected override string Name => "branch";

        public BlockNode CtrlNode => (BlockNode) InNodes[0]!;

        public Node Predicate => InNodes[1]!;

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