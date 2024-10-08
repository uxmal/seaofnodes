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


        public ReturnNode Return(BlockNode blockNode)
        {
            return new ReturnNode(++nextNodeId, blockNode);
        }

        public ReturnNode Return(BlockNode blockNode, Node? retVal)
        {
            return new ReturnNode(++nextNodeId, blockNode, retVal);
        }


        internal UseNode Use(Storage storage, BlockNode blockNode, Node node)
        {
            return new UseNode(++nextNodeId, storage, blockNode, node);
        }

    }
}
