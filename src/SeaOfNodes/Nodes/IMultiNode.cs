using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaOfNodes.Nodes
{
    /// <summary>
    /// Nodes implementing this interface have multiple outputs.
    /// A <see cref="ProjectionNode"/> is needed to pick one of these.
    /// </summary>
    public interface IMultiNode : INode
    {
        /// <summary>
        /// Given an index, get the index'th projection
        /// node associated with this node.
        /// </summary>
        public ProjectionNode? Project(int index)
        {
            foreach (var node in this.OutNodes)
            {
                if (node is ProjectionNode p && p.Index == index)
                    return p;
            }
            return null;
        }

        /// <summary>
        /// Given an index, get the index'th projection
        /// node associated with this node, as a CFProjectionNode.
        /// </summary>
        public CFProjectionNode? CFProject(int index)
        {
            foreach (var node in this.OutNodes)
            {
                if (node is CFProjectionNode cfp && cfp.Index == index)
                    return cfp;
            }
            return null;
        }
    }
}
