namespace SeaOfNodes.Nodes
{
    public interface INode
    {
        /// <summary>
        /// Input nodes, the reaching definitions
        /// of this node, may be null. The first
        /// node, by convention, is the control
        /// node that dominates this node.
        /// </summary>
        IReadOnlyList<Node?> InNodes { get; }

        /// <summary>
        /// The nodes that depend on the output value of this node.
        /// </summary>
        IReadOnlyList<Node> OutNodes { get; }
    }
}