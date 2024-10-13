using SeaOfNodes.Types;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace SeaOfNodes.Nodes
{
    /// <summary>
    /// This abstract base class provides common 
    /// functionality for all nodes in the IR.
    /// </summary>
    public abstract class Node : INode
    {
        private readonly List<Node?> inNodes;
        private readonly List<Node> outNodes;

        public Node(int nodeId, params Node?[] inNodes)
        {
            this.NodeId = nodeId;
            this.inNodes = new List<Node?>(inNodes);
            this.outNodes = [];
            AddUseDefEdges(this.inNodes);
        }

        public Node(int nodeId, Node? node, params Node?[] inNodes)
        {
            this.NodeId = nodeId;
            this.inNodes = [node, .. inNodes];
            this.outNodes = [];
            AddUseDefEdges(this.inNodes);
        }

        /// <summary>
        /// The id uniquely identifying this node.
        /// </summary>
        public int NodeId { get; }

        /// <summary>
        /// True if this node is has no out nodes.
        /// </summary>
        public bool IsUnused => outNodes.Count == 0;

        /// <summary>
        /// A node is dead if it has neither uses nor users.
        /// </summary>
        public bool IsDead => IsUnused && this.InNodes.Count == 0;


        /// <summary>
        /// Input nodes, the reaching definitions
        /// of this node, may be null. The first
        /// node, by convention, is the control
        /// node that dominates this node.
        /// </summary>
        public IReadOnlyList<Node?> InNodes => inNodes;

        /// <summary>
        /// The nodes that depend on the output value of this node.
        /// </summary>
        public IReadOnlyList<Node> OutNodes => outNodes;

        protected abstract string Name { get; }

        public NodeType? Type { get; }

        public string Label() => this.Name;

        public abstract T Accept<T>(INodeVisitor<T> visitor);
        public abstract T Accept<T, C>(INodeVisitor<T, C> visitor, C context);

        /// <summary>
        /// Adds an additional input to this node.
        /// </summary>
        /// <param name="use"></param>
        public void AddInput(Node? use)
        {
            this.inNodes.Add(use);
        }

        private void AddUseDefEdges(List<Node?> inNodes)
        {
            foreach (Node? inNode in inNodes)
            {
                inNode?.AddUse(this);
            }
        }

        public void SetInput(int i, Node same)
        {
            throw new NotImplementedException();
        }


        public TextWriter Write(TextWriter writer)
        {
            var visited = new HashSet<Node>();
            return Write(writer, visited);
        }

        protected internal TextWriter Write(TextWriter writer, HashSet<Node> visited) {
            if (visited.Contains(this))
            {
                writer.Write(Name);
                return writer;
            }
            else
            {
                return DoWrite(writer, visited);
            }
        }

        protected abstract TextWriter DoWrite(TextWriter writer, HashSet<Node> visited);

        public void AddUse(Node node)
        {
            outNodes.Add(node);
        }

        public bool IsCFG() => this is CFNode;

        public CFNode Cfg(int index)
        {
            return (CFNode)InNodes[index]!;
        }

        public virtual bool IsMultiHead() { return false; }
        public virtual bool IsMultiTail() { return false; }

        public void ReplaceInput(Node oldNode, Node newNode)
        {
            for (int i = 0; i < inNodes.Count; ++i)
            {
                if (inNodes[i] == oldNode)
                    inNodes[i] = newNode;
            }
        }

        public void ClearUses()
        {
            this.outNodes.Clear();
        }


        public override string ToString()
        {
            return $"{this.Label()}:{NodeId}";
        }

        public void Pin()
        {
            this.outNodes.Add(null!);
        }

        public void Unpin()
        {
            Debug.Assert(outNodes[^1] is null);
            this.outNodes.RemoveAt(outNodes.Count - 1);
        }

        public bool RemoveUse(Node use)
        {
            int iLast = outNodes.Count - 1;
            int i = 0;
            bool changed = false;
            for (; ; )
            {
                i = outNodes.IndexOf(use, i);
                if (i < 0)
                    return changed;
                changed = true;
                if (i < iLast)
                {
                    outNodes[i] = outNodes[iLast];
                }
                outNodes.RemoveAt(iLast);
                --iLast;
            }
        }
    }
}
