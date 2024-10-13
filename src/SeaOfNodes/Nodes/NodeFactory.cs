using Reko.Core;
using Reko.Core.Expressions;
using Reko.Core.Operators;
using Reko.Core.Types;

namespace SeaOfNodes.Nodes;

/// <summary>
/// This class creates <see cref="Node">Nodes</see> with unique 
/// node ID's.
/// </summary>
public class NodeFactory
{
    private int nextNodeId;

    public NodeFactory(Procedure proc)
    {
        // StartNode always has node ID 1. We avoid 0 to detect
        // uninitialized values.
        this.StartNode = new StartNode(NextId(), proc);
        this.StopNode = new StopNode(NextId(), StartNode);
    }

    public StartNode StartNode { get; }
    public StopNode StopNode { get; }

    public Node Binary(DataType dataType, BinaryOperator @operator, Node leftNode, Node rightNode)
    {
        return new BinaryNode(NextId(), dataType, @operator, leftNode, rightNode);
    }

    public BlockNode Block(Block block)
    {
        return new BlockNode(NextId(), block);
    }

    public BranchNode Branch(CFNode ctrlNode, Node predicate)
    {
        return new BranchNode(NextId(),  ctrlNode, predicate);
    }

    public CallNode Call(CFNode bn, Node fn)
    {
        return new CallNode(NextId(), bn, fn);
    }

    public ConditionOfNode ConditionOf(ConditionOf cof, Node expr)
    {
        return new ConditionOfNode(NextId(), expr);
    }

    public ConstantNode Constant(Constant value)
    {
        return new ConstantNode(NextId(), StartNode, value);
    }

    public DefNode Def(Storage stg)
    {
        return new DefNode(NextId(), StartNode, stg);
    }

    public DefNode Def(CFNode node, Storage stg)
    {
        return new DefNode(NextId(), node, stg);
    }

    public MemoryAccessNode Mem(DataType dataType, Node memId, Node effectiveAddress)
    {
        return new MemoryAccessNode(NextId(), dataType, memId, effectiveAddress);
    }

    public PhiNode Phi(Block block, params Node[] nodes)
    {
        return new PhiNode(NextId(), block, nodes);
    }

    public ProcedureConstantNode ProcedureConstant(ProcedureConstant pc)
    {
        return new ProcedureConstantNode(NextId(), StartNode, pc);
    }

    public CFProjectionNode CFProject(IMultiNode node, int index, string name)
    {
        return new CFProjectionNode(NextId(), node, index, name);
    }

    public ProjectionNode Project(IMultiNode node, int index, string name)
    {
        return new ProjectionNode(NextId(), node, index, name);
    }

    public ReturnNode Return(CFNode ctrlNode)
    {
        return new ReturnNode(NextId(), ctrlNode);
    }

    public ReturnNode Return(CFNode ctrlNode, Node? retVal)
    {
        return new ReturnNode(NextId(), ctrlNode, retVal);
    }

    public Node Seq(params Node[] nodes)
    {
        return new SequenceNode(NextId(), nodes);
    }

    public Node Slice(Node node, DataType dataType, ulong bitOffset)
    {
        return new SliceNode(NextId(), node, dataType, (int)bitOffset);
    }

    public UnaryNode Unary(DataType dataType, UnaryOperator @operator, Node expNode)
    {
        return new UnaryNode(NextId(), dataType, @operator, null, expNode);
    }

    public UseNode Use(Storage storage, CFNode ctrlNode, Node node)
    {
        return new UseNode(NextId(), storage, ctrlNode, node);
    }

    public StoreNode Store(DataType dataType, CFNode memoryId, Node eaNode, Node srcNode)
    {
        return new StoreNode(NextId(), dataType, memoryId, eaNode, srcNode);
    }

    private int NextId()
    {
        return ++nextNodeId;
    }
}
