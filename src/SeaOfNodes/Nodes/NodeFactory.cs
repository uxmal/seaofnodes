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

        public BranchNode Branch(CFNode ctrlNode, Node predicate)
        {
            return new BranchNode(++nextNodeId,  ctrlNode, predicate);
        }

        public CallNode Call(CFNode bn, Node fn)
        {
            return new CallNode(++nextNodeId, bn, fn);
        }

        public ConstantNode Constant(Constant value)
        {
            return new ConstantNode(++nextNodeId, StartNode, value);
        }

        public DefNode Def(Storage stg)
        {
            return new DefNode(++nextNodeId, StartNode, stg);
        }

        public DefNode Def(CFNode node, Storage stg)
        {
            return new DefNode(++nextNodeId, node, stg);
        }

        public MemoryAccessNode Mem(DataType dataType, Node memId, Node effectiveAddress)
        {
            return new MemoryAccessNode(++nextNodeId, dataType, memId, effectiveAddress);
        }

        public PhiNode Phi(Block block, params Node[] nodes)
        {
            return new PhiNode(++nextNodeId, block, nodes);
        }

        public ProcedureConstantNode ProcedureConstant(ProcedureConstant pc)
        {
            return new ProcedureConstantNode(++nextNodeId, StartNode, pc);
        }

        public Node Project(IMultiNode node, int index)
        {
            return new ProjectionNode(++nextNodeId, node, index);
        }

        public ReturnNode Return(CFNode ctrlNode)
        {
            return new ReturnNode(++nextNodeId, ctrlNode);
        }

        public ReturnNode Return(CFNode ctrlNode, Node? retVal)
        {
            return new ReturnNode(++nextNodeId, ctrlNode, retVal);
        }

        public Node Seq(params Node[] nodes)
        {
            return new SequenceNode(++nextNodeId, nodes);
        }

        public Node Slice(Node node, DataType dataType, ulong bitOffset)
        {
            return new SliceNode(++nextNodeId, node, dataType, (int)bitOffset);
        }

        public UnaryNode Unary(DataType dataType, UnaryOperator @operator, Node expNode)
        {
            return new UnaryNode(++nextNodeId, dataType, @operator, null, expNode);
        }

        public UseNode Use(Storage storage, CFNode ctrlNode, Node node)
        {
            return new UseNode(++nextNodeId, storage, ctrlNode, node);
        }

    }
}
