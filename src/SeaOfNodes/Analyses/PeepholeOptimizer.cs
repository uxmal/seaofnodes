using Reko.Core.Collections;
using Reko.Core.Diagnostics;
using SeaOfNodes.Nodes;
using System.Diagnostics;

namespace SeaOfNodes.Analyses;

/// <summary>
/// Performs peephole optimizations for each type of <see cref="Node"/>.
/// Each method returns null if no change happened, or the
/// resulting new node if an optimization occurred.
/// </summary>
/// <remarks>
/// This class is dependent on being invoked by the <see cref="NodeValuePropagator"/>:
/// in particular the freshly created nodes need to be connected 
/// to the other nodes in the graph.
/// </remarks>
public class PeepholeOptimizer : INodeVisitor<Node?>
{
    private static readonly TraceSwitch trace = new("Peep", nameof(PeepholeOptimizer))
    {
        Level = TraceLevel.Verbose
    };

    private readonly NodeFactory factory;

    public PeepholeOptimizer(NodeFactory factory)
    {
        this.factory = factory;
    }

    public Node? VisitBinaryNode(BinaryNode bin)
    {
        trace.Verbose($"Peep: {bin}");
        var left = bin.Left;
        var right = bin.Right;
        var cleft = left as ConstantNode;
        var cright = right as ConstantNode;
        if (cleft is not null && cright is not null)
        {
            var c = bin.Operator.ApplyConstants(bin.DataType, cleft.Value, cright.Value);
            return factory.Constant(c);
        }
        switch (bin.Operator.Type)
        {
        default:
        throw new NotImplementedException();
        }
    }

    public Node? VisitBlockNode(BlockNode block)
    {
        trace.Verbose($"Peep: {block}");
        return null;
    }

    public Node? VisitBranchNode(BranchNode branch)
    {
        throw new NotImplementedException();
    }

    public Node? VisitCallNode(CallNode call)
    {
        throw new NotImplementedException();
    }

    public Node? VisitConstantNode(ConstantNode c)
    {
        trace.Verbose($"Peep: {c}");
        return null;
    }

    public Node? VisitDefNode(DefNode def)
    {
        throw new NotImplementedException();
    }

    public Node? VisitMemoryAccessNode(MemoryAccessNode access)
    {
        throw new NotImplementedException();
    }

    public Node? VisitPhiNode(PhiNode phi)
    {
        throw new NotImplementedException();
    }

    public Node? VisitProcedureConstantNode(ProcedureConstantNode proc)
    {
        throw new NotImplementedException();
    }

    public Node? VisitProjectionNode(ProjectionNode projection)
    {
        throw new NotImplementedException();
    }

    public Node? VisitReturnNode(ReturnNode ret)
    {
        trace.Verbose($"Peep: {ret}");
        return null;
    }

    public Node? VisitSequenceNode(SequenceNode sequence)
    {
        throw new NotImplementedException();
    }

    public Node? VisitSliceNode(SliceNode slice)
    {
        throw new NotImplementedException();
    }

    public Node? VisitStartNode(StartNode start)
    {
        trace.Verbose($"Peep: {nameof(StartNode)}");
        return null;
    }

    public Node? VisitStopNode(StopNode stop)
    {
        trace.Verbose(nameof(StopNode));
        return null;
    }

    public Node? VisitUnaryNode(UnaryNode unary)
    {
        throw new NotImplementedException();
    }

    public Node? VisitUseNode(UseNode use)
    {
        trace.Verbose($"Peep: {nameof(UseNode)}");
        return null;
    }
}