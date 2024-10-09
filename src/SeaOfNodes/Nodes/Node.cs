using SeaOfNodes.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            this.outNodes = new List<Node>();
            AddUseDefEdges(this.inNodes);
        }

        public Node(int nodeId, Node? node, params Node?[] inNodes)
        {
            this.NodeId = nodeId;
            this.inNodes = [node, .. inNodes];
            this.outNodes = [];
            AddUseDefEdges(this.inNodes);
        }

        public int NodeId { get; }

        public bool IsUnused => outNodes.Count == 0;

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

        public string label() => this.Name;

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

        #region Peephole infrastructure

        public Node Peephole()
        {
            var nodeNew = DoPeephole();
            if (nodeNew is null)
                return this;
            return EliminateDeadCode(nodeNew);
        }

        public Node? DoPeephole()
        {
            var newNode = Simplify();
            if (newNode is not null)
                return newNode;
            //$TODO: Count idle?
            return null;
        }

        protected abstract Node? Simplify();

        private Node EliminateDeadCode(Node nodeNew)
        {
            if (nodeNew != this &&
                this.IsUnused &&
                !this.IsDead)
            {
                this.Kill();
            }
            return nodeNew;
        }

        private void Kill()
        {
            throw new NotImplementedException();
        }

        #endregion

        public bool IsCFG() => this is CFNode;

        public CFNode Cfg(int index)
        {
            return (CFNode)InNodes[index]!;
        }

        public virtual bool IsMultiHead() { return false; }
        public virtual bool IsMultiTail() { return false; }
    }
}
