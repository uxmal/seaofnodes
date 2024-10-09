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
        public ProjectionNode? Project(int index)
        {
            foreach (var node in this.OutNodes)
            {
                if (node is ProjectionNode p && p.Index == index)
                    return p;
            }
            return null;
        }
    }
}
