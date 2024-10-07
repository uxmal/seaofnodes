


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