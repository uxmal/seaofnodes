using Reko.Core.Collections;
using Reko.Core.Graphs;
using SeaOfNodes.Nodes;

namespace SeaOfNodes.Analyses;

/// <summary>
/// Perform value propagation on the node value graph.
/// </summary>
/// <remarks>
/// This analysis works in conjunction with the <see cref="PeepholeOptimizer"/>
/// to improve the shape of the graph.
/// </remarks>
public class NodeValuePropagator
{
    private readonly WorkList<Node> worklist;
    private readonly PeepholeOptimizer peephole;

    public NodeValuePropagator(NodeFactory factory) 
    {
        this.worklist = new WorkList<Node>();
        this.peephole = new PeepholeOptimizer(factory);
    }

    /// <summary>
    /// Perform the value propagation analysis on the node graph.
    /// </summary>
    /// <param name="node">'Root' node from which to start the analysis.
    /// Typically, this is the 'Stop' node.
    /// </param>
    /// <returns>A new node graph on which value propagation and dead
    /// node removal has taken place.
    /// </returns>
    public Node Transform(Node startNode)
    {
        var dfs = new DfsIterator<Node>(new GraphAdapter());
        worklist.AddRange(dfs.PostOrder(startNode));
        while (worklist.TryGetWorkItem(out var n))
        {
            if (n.IsUnused && n is not StopNode)
            {
                Remove(n);
            }
            else
            {
                var newNode = n.Accept(peephole);
                if (newNode is null)
                    continue;
                Replace(n, newNode);
            }
        }
        return startNode;
    }

    /// <summary>
    /// Remove a node which has no uses from the node graph.
    /// </summary>
    /// <param name="n">Node to remove</param>
    /// <remarks>
    /// Each input of <paramref name="n"/> is disconnected,
    /// and then put on the worklist as removals may result
    /// in cascades of dead nodes.
    /// </remarks>
    private void Remove(Node n)
    {
        foreach (var def in n.InNodes)
        {
            if (def is null)
                continue;
            if (def.RemoveUse(n))
                worklist.Add(def);
        }
    }

    /// <summary>
    /// Replaces all uses of <paramref name="oldNode"/> 
    /// with <paramref name="newNode" /> in the node graph.
    /// </summary>
    /// <param name="oldNode"></param>
    /// <param name="newNode"></param>
    /// <returns></returns>
    private Node Replace(Node oldNode, Node newNode)
    {
        var outNodes = oldNode.OutNodes;
        // Each user gets replaced and put back on the 
        // worklist.
        for (int i = 0; i < outNodes.Count; ++i)
        {
            var use = outNodes[i];
            use.ReplaceInput(oldNode, newNode);
            newNode.AddUse(use);
            worklist.Add(use);
        }
        oldNode.ClearUses();
        Remove(oldNode);
        return newNode;
    }
}