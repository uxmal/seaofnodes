using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaOfNodes.Nodes
{
    public class NodeEqualityComparer : IEqualityComparer<Node>
    {
        private readonly NodeEqualityVisitor eq;
        private readonly NodeHasher hasher;
        private readonly Dictionary<Node, int> hashCache;

        public NodeEqualityComparer()
        {
            this.eq = new();
            this.hasher = new();
            this.hashCache = [];
        }

        public bool Equals(Node? x, Node? y)
        {
            if (x is null)
                return y is null;
            if (y is null)
                return x is null;
            if (x.InNodes.Count != y.InNodes.Count)
                return false;
            if (!x.Accept<bool, Node>(eq, y))
                return false;
            for (int i = 0; i < x.InNodes.Count; ++i)
            {
                if (x.InNodes[i] != y.InNodes[i])
                    return false;
            }
            return true;
        }

        public int GetHashCode([DisallowNull] Node node)
        {
            if (hashCache.TryGetValue(node, out var h))
                return h;
            h = node.Accept(this.hasher);
            foreach (var i in node.InNodes)
            {
                h *= 17;
                if (i is not null)
                    h ^= i.GetHashCode();
            }
            hashCache.Add(node, h);
            return h;
        }


        private class NodeEqualityVisitor : INodeVisitor<bool, Node>
        {
            public bool VisitBinaryNode(BinaryNode bin, Node ctx)
            {
                throw new NotImplementedException();
            }

            public bool VisitBlockNode(BlockNode block, Node ctx)
            {
                throw new NotImplementedException();
            }

            public bool VisitBranchNode(BranchNode branch, Node ctx)
            {
                throw new NotImplementedException();
            }

            public bool VisitCallNode(CallNode call, Node ctx)
            {
                throw new NotImplementedException();
            }

            public bool VisitCFProjectionNode(CFProjectionNode projection, Node other)
            {
                throw new NotImplementedException();
            }

            public bool VisitConditionOf(ConditionOfNode cond, Node ctx)
            {
                throw new NotImplementedException();
            }

            public bool VisitConstantNode(ConstantNode c, Node ctx)
            {
                throw new NotImplementedException();
            }

            public bool VisitDefNode(DefNode def, Node ctx)
            {
                throw new NotImplementedException();
            }

            public bool VisitMemoryAccessNode(MemoryAccessNode access, Node ctx)
            {
                throw new NotImplementedException();
            }

            public bool VisitPhiNode(PhiNode phi, Node ctx)
            {
                throw new NotImplementedException();
            }

            public bool VisitProcedureConstantNode(ProcedureConstantNode proc, Node ctx)
            {
                throw new NotImplementedException();
            }

            public bool VisitProjectionNode(ProjectionNode projection, Node ctx)
            {
                throw new NotImplementedException();
            }

            public bool VisitReturnNode(ReturnNode ret, Node ctx)
            {
                throw new NotImplementedException();
            }

            public bool VisitSequenceNode(SequenceNode sequence, Node ctx)
            {
                throw new NotImplementedException();
            }

            public bool VisitSliceNode(SliceNode slice, Node ctx)
            {
                throw new NotImplementedException();
            }

            public bool VisitStartNode(StartNode start, Node ctx)
            {
                throw new NotImplementedException();
            }

            public bool VisitStopNode(StopNode stop, Node ctx)
            {
                throw new NotImplementedException();
            }

            public bool VisitStoreNode(StoreNode store, Node ctx)
            {
                throw new NotImplementedException();
            }

            public bool VisitUnaryNode(UnaryNode unary, Node ctx)
            {
                throw new NotImplementedException();
            }

            public bool VisitUseNode(UseNode use, Node ctx)
            {
                throw new NotImplementedException();
            }
        }

        private class NodeHasher : INodeVisitor<int>
        {
            public int VisitBinaryNode(BinaryNode bin)
            {
                throw new NotImplementedException();
            }

            public int VisitBlockNode(BlockNode block)
            {
                throw new NotImplementedException();
            }

            public int VisitBranchNode(BranchNode branch)
            {
                throw new NotImplementedException();
            }

            public int VisitCallNode(CallNode call)
            {
                throw new NotImplementedException();
            }

            public int VisitCFProjectionNode(CFProjectionNode projection)
            {
                throw new NotImplementedException();
            }

            public int VisitConditionOf(ConditionOfNode c)
            {
                throw new NotImplementedException();
            }

            public int VisitConstantNode(ConstantNode c)
            {
                throw new NotImplementedException();
            }

            public int VisitDefNode(DefNode def)
            {
                throw new NotImplementedException();
            }

            public int VisitMemoryAccessNode(MemoryAccessNode access)
            {
                throw new NotImplementedException();
            }

            public int VisitPhiNode(PhiNode phi)
            {
                throw new NotImplementedException();
            }

            public int VisitProcedureConstantNode(ProcedureConstantNode proc)
            {
                throw new NotImplementedException();
            }

            public int VisitProjectionNode(ProjectionNode projection)
            {
                throw new NotImplementedException();
            }

            public int VisitReturnNode(ReturnNode ret)
            {
                throw new NotImplementedException();
            }

            public int VisitSequenceNode(SequenceNode sequence)
            {
                throw new NotImplementedException();
            }

            public int VisitSliceNode(SliceNode slice)
            {
                throw new NotImplementedException();
            }

            public int VisitStartNode(StartNode start)
            {
                throw new NotImplementedException();
            }

            public int VisitStopNode(StopNode stop)
            {
                throw new NotImplementedException();
            }

            public int VisitStoreNode(StoreNode store)
            {
                throw new NotImplementedException();
            }

            public int VisitUnaryNode(UnaryNode unary)
            {
                throw new NotImplementedException();
            }

            public int VisitUseNode(UseNode use)
            {
                throw new NotImplementedException();
            }
        }
    }
}
