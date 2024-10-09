using Reko.Core.Expressions;

namespace SeaOfNodes.Nodes
{
    public class ProcedureConstantNode : Node
    {
        public ProcedureConstantNode(int nodeId, StartNode startNode, ProcedureConstant pc)
            : base(nodeId, startNode)
        {
            this.Procedure = pc;
        }

        public ProcedureConstant Procedure { get; }

        protected override string Name => Procedure.Procedure.Name;

        protected override TextWriter DoWrite(TextWriter writer, HashSet<Node> visited)
        {
            throw new NotImplementedException();
        }

        protected override Node? Simplify()
        {
            throw new NotImplementedException();
        }
    }
}