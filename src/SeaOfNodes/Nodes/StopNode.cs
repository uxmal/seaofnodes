


namespace SeaOfNodes.Nodes
{
    /// <summary>
    /// Represents the end of the procedure. Its inputs are the exit node all the uses 
    /// </summary>
    public class StopNode : CFNode
    {
        public StopNode(int nodeId, StartNode start) : base(nodeId, start)
        {
        }

        protected override string Name => "Stop";

        public override T Accept<T>(INodeVisitor<T> visitor)
        {
            return visitor.VisitStopNode(this);
        }

        public override T Accept<T, C>(INodeVisitor<T, C> visitor, C ctx)
        {
            return visitor.VisitStopNode(this, ctx);
        }
        protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
        {
            writer.Write("Stop[ ");
            foreach (var inNode in this.InNodes)
            {
                inNode?.Write(writer, visited).Write(' ');
            }
            writer.Write(" ]");
            return writer;
        }

        protected override Node? Simplify()
        {
            return null;
        }
    }
}