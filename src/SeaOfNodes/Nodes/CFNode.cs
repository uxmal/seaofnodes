
namespace SeaOfNodes.Nodes
{

    /// <summary>
    /// A control-flow node.
    /// </summary>
    public abstract class CFNode : Node
    {
        public CFNode(int nodeId, params Node[] nodes)
            : base(nodeId, nodes)
        {
        }

        internal bool blockHead()
        {
            throw new NotImplementedException();
        }

        internal int idepth()
        {
            throw new NotImplementedException();
        }


    }
}