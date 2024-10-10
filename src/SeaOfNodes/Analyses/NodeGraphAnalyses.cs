using SeaOfNodes.Nodes;

namespace SeaOfNodes.Analyses;

/// <summary>
/// Extension methods that extend the <see cref="Nodes.Node"/> class with
/// methods that perform analyses (and optmizations) of the node graph.
/// </summary>
public static class NodeGraphAnalyses
{
    public static Node Propagate(this Node node, NodeFactory factory)
    {
        var vp = new NodeValuePropagator(factory);
        node = vp.Transform(node);
        return node;
    }
}
