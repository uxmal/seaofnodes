using Reko.Core;
using Reko.Core.Expressions;

namespace SeaOfNodes.Nodes
{

    public class NodeFactory
    {
        private int nextNodeId;

        public NodeFactory()
        {
            this.StartNode = new StartNode(++nextNodeId);
            this.StopNode = new StopNode(++nextNodeId, StartNode);
        }

        public StartNode StartNode { get; }
        public StopNode StopNode { get; }

        public ConstantNode Constant(Constant value)
        {
            return new ConstantNode(++nextNodeId, StartNode, value);
        }

        public PhiNode Phi(Block block, params Node[] nodes)
        {
            return new PhiNode(++nextNodeId, block, nodes);
        }

        public BlockNode Block(Block block)
        {
            return new BlockNode(++nextNodeId, block);
        }

        internal UseNode Use(Storage storage, BlockNode blockNode, Node node)
        {
            return new UseNode(++nextNodeId, storage, blockNode, node);
        }

        public ReturnNode Return(BlockNode blockNode)
        {
            return new ReturnNode(++nextNodeId, blockNode);
        }

        public ReturnNode Return(BlockNode blockNode, Node? retVal)
        {
            return new ReturnNode(++nextNodeId, blockNode, retVal);
        }

    }
}
