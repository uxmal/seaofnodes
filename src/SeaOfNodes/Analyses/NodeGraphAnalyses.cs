using SeaOfNodes.Nodes;

namespace SeaOfNodes.Analyses;

/// <summary>
/// Extension methods that extend the <see cref="Nodes.Node"/> class with
/// methods that perform analyses (and optmizations) of the node graph.
/// </summary>
public static class NodeGraphAnalyses
{
    /// <summary>
    /// Apply value propagation to the given node.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="factory"></param>
    /// <returns></returns>
    public static Node Propagate(this Node node, NodeFactory factory)
    {
        var vp = new NodeValuePropagator(factory);
        node = vp.Transform(node);
        return node;
    }

    public static Node Peephole(this Node node, NodeFactory factory)
    {
        var peep = new PeepholeOptimizer(factory);
        var newNode = node.Accept(peep);
        return newNode is null
            ? node
            : newNode;
    }
}
