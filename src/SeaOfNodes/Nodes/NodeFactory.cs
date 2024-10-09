using Reko.Core;
using Reko.Core.Expressions;
using Reko.Core.Operators;
using Reko.Core.Types;

namespace SeaOfNodes.Nodes
{
    public class NodeFactory
    {
        private int nextNodeId;

        public NodeFactory()
        {
            // StartNode always has node ID 1. We avoid 0 to detect
            // uninitialized values.
            this.StartNode = new StartNode(++nextNodeId);
            this.StopNode = new StopNode(++nextNodeId, StartNode);
        }

        public StartNode StartNode { get; }
        public StopNode StopNode { get; }

        public Node Binary(DataType dataType, BinaryOperator @operator, Node leftNode, Node rightNode)
        {
            return new BinaryNode(++nextNodeId, dataType, @operator, leftNode, rightNode);
        }

        public BlockNode Block(Block block)
        {
            return new BlockNode(++nextNodeId, block);
        }

        public BranchNode Branch(BlockNode ctrlNode, Node predicate)
        {
            return new BranchNode(++nextNodeId,  ctrlNode, predicate);
        }

        public ConstantNode Constant(Constant value)
        {
            return new ConstantNode(++nextNodeId, StartNode, value);
        }

        public DefNode Def(Storage stg)
        {
            return new DefNode(++nextNodeId, StartNode, stg);
        }


        public PhiNode Phi(Block block, params Node[] nodes)
        {
            return new PhiNode(++nextNodeId, block, nodes);
        }

        public Node Project(IMultiNode node, int index)
        {
            return new ProjectionNode(++nextNodeId, node, index);
        }

        public ReturnNode Return(BlockNode blockNode)
        {
            return new ReturnNode(++nextNodeId, blockNode);
        }

        public ReturnNode Return(BlockNode blockNode, Node? retVal)
        {
            return new ReturnNode(++nextNodeId, blockNode, retVal);
        }

        public UnaryNode Unary(DataType dataType, UnaryOperator @operator, Node expNode)
        {
            return new UnaryNode(++nextNodeId, dataType, @operator, null, expNode);
        }

        public UseNode Use(Storage storage, BlockNode blockNode, Node node)
        {
            return new UseNode(++nextNodeId, storage, blockNode, node);
        }
    }
}
