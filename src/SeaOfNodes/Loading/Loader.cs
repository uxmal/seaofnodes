using Reko.Core;
using Reko.Core.Code;
using Reko.Core.Collections;
using Reko.Core.Expressions;
using Reko.Core.Graphs;
using SeaOfNodes.Nodes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SeaOfNodes.Loading
{
    /// <summary>
    /// Loads a Reko <see cref="Procedure"/> into SeaOfNodes representation.
    /// </summary>
    public class Loader : InstructionVisitor<Node>, ExpressionVisitor<Node>
    {
        private NodeFactory factory;
        private Dictionary<Block, BlockState> states;
        private Dictionary<Block, BlockNode> blockNodes;
        private HashSet<Block> sealedBlocks;
        private Block? blockCur;
        private HashSet<Storage> definedStorages;
        private HashSet<Storage> usedStorages;
        private BlockGraph cfg;

        public Loader()
        {
            this.factory = new NodeFactory();
            this.states = [];
            this.blockNodes = [];
            this.definedStorages = [];
            this.usedStorages = [];
            this.sealedBlocks = [];
            this.cfg = default!;
        }

        public StopNode Load(Procedure procedure)
        {
            this.cfg = procedure.ControlGraph;
            sealedBlocks.Add(procedure.ExitBlock);
            CreateBlockNodes(this.cfg);
            var wl = new WorkList<Block>();
            wl.Add(procedure.EntryBlock);
            AddEdge(factory.StartNode, blockNodes[procedure.EntryBlock]);
            var visited = new HashSet<Block>();
            while (wl.TryGetWorkItem(out var block))
            {
                if (!visited.Add(block))
                    continue;
                this.blockCur = block;
                states.Add(block, new BlockState());
                ProcessBlock(block);
                foreach (var succ in procedure.ControlGraph.Successors(block))
                {
                    if (blockCur is not null)
                    {
                        AddEdge(blockNodes[blockCur], blockNodes[succ]);
                    }
                    wl.Add(succ);
                }
                sealedBlocks.Add(block);
            }
            ProcessExitBlock(procedure.ExitBlock);
            return factory.StopNode;
        }

        private void AddEdge(Node def, Node use)
        {
            def.AddUse(use);
            use.AddInput(def);
        }

        private void CreateBlockNodes(BlockGraph cfg)
        {
            foreach (var b in cfg.Blocks)
            {
                blockNodes.Add(b, factory.Block(b));
            }
        }

        /*
        private void ProcessBlockEdge(Block pred, Block succ)
        {
            var nPred = blockNodes[pred];
            var nSucc = blockNodes[succ];

            nSucc.AddInput(nPred);
            nPred.AddUse(nSucc);
        }*/

        private void ProcessBlock(Block block)
        {
            foreach (var stm in block.Statements)
            {
                var node = stm.Instruction.Accept(this);
            }
        }

        private void ProcessExitBlock(Block exitBlock)
        {
            var blockCur = exitBlock;
            AddEdge(blockNodes[exitBlock], factory.StopNode);
            foreach (var stg in definedStorages.Except(usedStorages))
            {
                var node = ReadStorage(stg, blockCur);
                var use = factory.Use(stg, blockNodes[exitBlock], node);
                AddEdge(use, factory.StopNode);
                usedStorages.Add(stg);
            }
        }

        public Node VisitAddress(Address addr)
        {
            throw new NotImplementedException();
        }

        public Node VisitApplication(Application appl)
        {
            throw new NotImplementedException();
        }

        public Node VisitArrayAccess(ArrayAccess acc)
        {
            throw new NotImplementedException();
        }

        public Node VisitAssignment(Assignment ass)
        {
            Debug.Assert(blockCur is not null);
            var srcNode = ass.Src.Accept(this);
            WriteStorage(ass.Dst, blockCur, srcNode);
            return srcNode;
        }

        public Node VisitBinaryExpression(BinaryExpression binExp)
        {
            var leftNode = binExp.Left.Accept(this);
            var rightNode = binExp.Right.Accept(this);
            var binNode = factory.Binary(binExp.DataType, binExp.Operator, leftNode, rightNode);
            return binNode;
        }

        public Node VisitBranch(Branch branch)
        {
            Debug.Assert(blockCur is not null);
            var bn = blockNodes[blockCur];
            var predicate = branch.Condition.Accept(this);
            var branchNode = factory.Branch(bn, predicate);
            var falseProj = factory.Project(branchNode, 0);
            var trueProj = factory.Project(branchNode, 1);
            AddEdge(falseProj, blockNodes[blockCur.Succ[0]]);
            AddEdge(trueProj, blockNodes[blockCur.Succ[1]]);
            return branchNode;
        }

        public Node VisitCallInstruction(CallInstruction ci)
        {
            throw new NotImplementedException();
        }

        public Node VisitCast(Cast cast)
        {
            throw new NotImplementedException();
        }

        public Node VisitComment(CodeComment comment)
        {
            throw new NotImplementedException();
        }

        public Node VisitConditionalExpression(ConditionalExpression cond)
        {
            throw new NotImplementedException();
        }

        public Node VisitConditionOf(ConditionOf cof)
        {
            throw new NotImplementedException();
        }

        public Node VisitConstant(Constant c)
        {
            var node = factory.Constant(c).Peephole();
            return node;
        }

        public Node VisitConversion(Conversion conversion)
        {
            throw new NotImplementedException();
        }

        public Node VisitDefInstruction(DefInstruction def)
        {
            throw new NotImplementedException();
        }

        public Node VisitDereference(Dereference deref)
        {
            throw new NotImplementedException();
        }

        public Node VisitFieldAccess(FieldAccess acc)
        {
            throw new NotImplementedException();
        }

        public Node VisitGotoInstruction(GotoInstruction gotoInstruction)
        {
            throw new NotImplementedException();
        }

        public Node VisitIdentifier(Identifier id)
        {
            Debug.Assert(blockCur is not null);
            var node = ReadStorage(id.Storage, blockCur);
            return node;
        }

        public Node VisitMemberPointerSelector(MemberPointerSelector mps)
        {
            throw new NotImplementedException();
        }

        public Node VisitMemoryAccess(MemoryAccess access)
        {
            throw new NotImplementedException();
        }

        public Node VisitMkSequence(MkSequence seq)
        {
            throw new NotImplementedException();
        }

        public Node VisitOutArgument(OutArgument outArgument)
        {
            throw new NotImplementedException();
        }

        public Node VisitPhiAssignment(PhiAssignment phi)
        {
            throw new NotImplementedException();
        }

        public Node VisitPhiFunction(PhiFunction phi)
        {
            throw new NotImplementedException();
        }

        public Node VisitPointerAddition(PointerAddition pa)
        {
            throw new NotImplementedException();
        }

        public Node VisitProcedureConstant(ProcedureConstant pc)
        {
            throw new NotImplementedException();
        }

        public Node VisitReturnInstruction(ReturnInstruction ret)
        {
            Debug.Assert(blockCur is not null);
            var retVal = ret.Expression?.Accept(this);
            var bn = blockNodes[blockCur];
            var retNode = factory.Return(bn, retVal);
            AddEdge(bn, retNode);
            return retNode;
        }

        public Node VisitScopeResolution(ScopeResolution scopeResolution)
        {
            throw new NotImplementedException();
        }

        public Node VisitSegmentedAddress(SegmentedPointer address)
        {
            throw new NotImplementedException();
        }

        public Node VisitSideEffect(SideEffect side)
        {
            throw new NotImplementedException();
        }

        public Node VisitSlice(Slice slice)
        {
            throw new NotImplementedException();
        }

        public Node VisitStore(Store store)
        {
            throw new NotImplementedException();
        }

        public Node VisitStringConstant(StringConstant str)
        {
            throw new NotImplementedException();
        }

        public Node VisitSwitchInstruction(SwitchInstruction si)
        {
            throw new NotImplementedException();
        }

        public Node VisitTestCondition(TestCondition tc)
        {
            throw new NotImplementedException();
        }

        public Node VisitUnaryExpression(UnaryExpression unary)
        {
            var expNode = unary.Expression.Accept(this);
            var unNode = factory.Unary(unary.DataType, unary.Operator, expNode);
            return unNode;
        }

        public Node VisitUseInstruction(UseInstruction use)
        {
            throw new NotImplementedException();
        }

        private void WriteStorage(Identifier dst, Block block, Node node)
        {
            WriteStorage(dst.Storage, block, node);
        }

        private void WriteStorage(Storage stg, Block block, Node node)
        {
            this.definedStorages.Add(stg);
            states[block].Definitions[stg] = node;
        }

        private Node ReadStorage(Storage stg, Block block)
        {
            if (TryReadLocalStorage(stg, block, out Node? local))
                return local;
            return ReadStorageRecursive(stg, block);
        }

        private Node ReadStorageRecursive(Storage stg, Block block)
        {
            var preds = block.Procedure.ControlGraph.Predecessors(block);
            if (preds.Count == 0)
            {
                // Live in parameter.
                var node = factory.Def(stg);
                return node;
            }
            if (block.Pred.Any(p => !sealedBlocks.Contains(p))) {
                // Incomplete CFG
                var phi = factory.Phi(block);
                AddEdge(blockNodes[block], phi);
                states[block].IncompletePhis[stg] = phi;
                WriteStorage(stg, block, phi);
                return phi;
            }
            Node? val;
            if (preds.Count == 1)
            {
                var pred = preds.First();
                // Optimize the common case of one predecessor: No phi needed
                if (!TryReadLocalStorage(stg, pred, out val))
                    val = ReadStorageRecursive(stg, pred);
            }
            else
            {
                // Break potential cycles with operandless phi
                var phi = factory.Phi(block);
                AddEdge(blockNodes[block], phi);
                WriteStorage(stg, block, phi);
                val = AddPhiOperands(stg, phi);
            }
            WriteStorage(stg, block, val);
            return val;
        }

        private Node AddPhiOperands(Storage variable, PhiNode phi)
        {
            // Determine operands from predecessors
            foreach (var pred in cfg.Predecessors(phi.Block))
            {
                var var = ReadStorage(variable, pred);
                phi.AddInput(var);
                var.AddUse(phi);
            }
            return TryRemoveTrivialPhi(phi, variable);
        }

        private Node TryRemoveTrivialPhi(PhiNode phi, Storage stg)
        {
            Node? same = null;
            foreach (var op in phi.InNodes.Skip(1))
            {
                if (op == same || op == phi)
                    continue;   // Unique value or self−reference
                if (same is not null)
                    return phi; // The phi merges at least two values: not trivial
                same = op;
            }
            // The phi is unreachable or in the start block
            if (same is null)
                same = factory.Def(stg);
            var users = phi.OutNodes.Where(n => n != phi); // Remember all users except the phi itself
            ReplaceBy(users, phi, same); // Reroute all uses of phi to same and remove phi
                                         // Try to recursively remove all phi users, which might have become trivial
            foreach (var use in users)
            {
                if (use is PhiNode usingPhi)
                    TryRemoveTrivialPhi(usingPhi, stg);
            }
            return same;
        }

        private void ReplaceBy(IEnumerable<Node> users, PhiNode phi, Node same)
        {
            foreach (var user in users)
            {
                for (int i = 0; i < user.InNodes.Count; ++i)
                {
                    if (user.InNodes[i] == phi)
                        user.SetInput(i, same);
                }
            }
        }

        private bool TryReadLocalStorage(Storage stg, Block block, [MaybeNullWhen(false)] out Node local)
        {
            var scope = states[block];
            if (scope.Definitions.TryGetValue(stg, out local))
            {
                return true;
            }
            local = null;
            return false;
            throw new NotImplementedException();
        }

        private class BlockState
        {
            public BlockState()
            {
                this.Definitions = [];
                this.IncompletePhis = [];
            }
            public Dictionary<Storage, Node> Definitions { get; }
            public Dictionary<Storage, PhiNode> IncompletePhis { get; internal set; }
        }
    }
}
