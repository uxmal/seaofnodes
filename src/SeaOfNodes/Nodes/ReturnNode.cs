
namespace SeaOfNodes.Nodes
{
    public class ReturnNode : CFNode
    {
        private Node? retVal;

        public ReturnNode(int nodeId, CFNode blockNode, Node? retVal = null)
            : base(nodeId)
        {
            this.retVal = retVal;
            AddInput(blockNode);
            if (retVal is not null)
                AddInput(retVal);
        }

        protected override string Name => "return";

        protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
        {
            writer.Write("return");
            if (retVal != null)
            {
                writer.Write(" ");
                retVal.Write(writer, visited);
            }
            return writer;
        }

        protected override Node? Simplify()
        {
            throw new NotImplementedException();
        }
    }
}