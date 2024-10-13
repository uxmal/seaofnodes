using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaOfNodes.Nodes;

public class SsaLikePrinter
    : INodeVisitor<StringBuilder, StringBuilder>
{
    public void WriteLine(Node node, StringBuilder sb)
    {
        node.Accept(this, sb);
    }

    public StringBuilder VisitBinaryNode(BinaryNode bin, StringBuilder sb)
    {
        var left = bin.InNodes[1]!;
        var right = bin.InNodes[2]!;
        sb.AppendLine($"    {RenderVar(bin.NodeId)} = {Render(left)} {bin.Operator} {Render(right)}");
        return sb;
    }

    public StringBuilder VisitBlockNode(BlockNode block, StringBuilder sb)
    {
        sb.AppendLine($"{block.Block.DisplayName}:");
        return sb;
    }

    public StringBuilder VisitBranchNode(BranchNode branch, StringBuilder sb)
    {
        var predicate = branch.InNodes[1];
        var target = ((BlockNode)branch.OutNodes[1]!).Block;
        sb.AppendLine($"    branch {Render(predicate)} {target.DisplayName}");
        return sb;
    }

    public StringBuilder VisitCallNode(CallNode call, StringBuilder sb)
    {
        var target = call.InNodes[1]!;
        sb.AppendLine($"    call {Render(target)}");
        var sep = "        ";
        bool emitLine = false;
        foreach (var o in call.OutNodes)
        {
            if (o is UseNode use)
            {
                sb.Append(sep);
                sb.Append(use.Storage);
                sb.Append(':');
                sb.Append(Render(use.InNodes[1]!));
                sep = ",";
                emitLine = true;
            }
        }
        if (emitLine)
            sb.AppendLine();
        emitLine = false;
        sep = "        ";
        foreach (var o in call.OutNodes)
        {
            if (o is DefNode def)
            {
                sb.Append(sep);
                sb.Append(def.Storage);
                sb.Append(':');
                sb.Append(Render(def.InNodes[1]!));
                sep = ",";
                emitLine = true;
            }
        }
        if (emitLine)
            sb.AppendLine();
        return sb;
    }

    public StringBuilder VisitCFProjectionNode(CFProjectionNode projection, StringBuilder sb)
    {
        return sb;
    }

    public StringBuilder VisitConditionOf(ConditionOfNode cond, StringBuilder sb)
    {
        sb.Append("    ");
        sb.Append(RenderVar(cond.NodeId));
        sb.Append(" = ");
        sb.Append("cond(");
        sb.Append(Render(cond.InNodes[1]!));
        sb.AppendLine(")");
        return sb;
    }

    public StringBuilder VisitConstantNode(ConstantNode c, StringBuilder sb)
    {
        //sb.AppendLine($"    {RenderVar(c.NodeId)} = {c.Value}");
        return sb;
    }

    public StringBuilder VisitDefNode(DefNode def, StringBuilder sb)
    {
        sb.AppendLine($"    {RenderVar(def.NodeId)} = {def.Storage}");
        return sb;
    }

    public StringBuilder VisitMemoryAccessNode(MemoryAccessNode access, StringBuilder sb)
    {
        var mem = access.InNodes[0]!;
        var ea = access.InNodes[1]!;
        sb.AppendLine($"    {RenderVar(access.NodeId)} = Mem{mem.NodeId}[{ea.NodeId}]");
        return sb;
    }

    public StringBuilder VisitPhiNode(PhiNode phi, StringBuilder sb)
    {
        sb.Append("    ");
        sb.Append(RenderVar(phi.NodeId));
        sb.Append(" = PHI(");
        var sep = "";
        foreach (var use in phi.InNodes.Skip(1))
        {
            sb.Append(sep);
            sb.Append(RenderVar(use!.NodeId));
            sep = ",";
        }
        sb.AppendLine(")");
        return sb;
    }

    public StringBuilder VisitProcedureConstantNode(ProcedureConstantNode proc, StringBuilder sb)
    {
        sb.Append($"    {Render(proc)} = {proc.Procedure.Procedure.Name}");
        return sb;
    }

    public StringBuilder VisitProjectionNode(ProjectionNode projection, StringBuilder sb)
    {
        return sb;
    }

    public StringBuilder VisitReturnNode(ReturnNode ret, StringBuilder sb)
    {
        sb.AppendLine("    return");
        return sb;
    }

    public StringBuilder VisitSequenceNode(SequenceNode sequence, StringBuilder sb)
    {
        sb.Append("    ");
        sb.Append(RenderVar(sequence.NodeId));
        sb.Append(" = SEQ(");
        var sep = "";
        foreach (var use in sequence.InNodes.Skip(1))
        {
            sb.Append(sep);
            sb.Append(Render(use));
            sep = ",";
        }
        sb.AppendLine(")");
        return sb;
    }

    public StringBuilder VisitSliceNode(SliceNode slice, StringBuilder sb)
    {
        var e = Render(slice);
        var dst = RenderVar(slice.NodeId);
        sb.AppendLine($"    {dst} = SLICE({e}, {slice.DataType}, {slice.BitOffset})");
        return sb;
    }

    public StringBuilder VisitStartNode(StartNode start, StringBuilder sb)
    {
        sb.AppendLine("def proc():");
        return sb;
    }

    public StringBuilder VisitStopNode(StopNode stop, StringBuilder sb)
    {
        return sb;
    }

    public StringBuilder VisitStoreNode(StoreNode store, StringBuilder sb)
    {
        var memOut = ((IMultiNode)store).Project(1)!.NodeId;
        var ea = store.InNodes[1];
        var src = store.InNodes[2];
        sb.AppendLine($"    Mem{memOut}[{Render(ea)}:{store.DataType}] = {Render(src)}");
        return sb;
    }

    public StringBuilder VisitUnaryNode(UnaryNode unary, StringBuilder sb)
    {
        sb.AppendLine($"    {RenderVar(unary.NodeId)} = {unary.Operator}{Render(unary.InNodes[1]!)}");
        return sb;
    }

    public StringBuilder VisitUseNode(UseNode use, StringBuilder sb)
    {
        var useVar = use.InNodes[1]!;
        sb.AppendLine($"    use {use.Storage}:{Render(useVar)}");
        return sb;
    }

    private string RenderVar(int nodeId)
    {
        return $"v{nodeId}";
    }

    private string Render(Node? n)
    {
        return n switch
        {
            ConstantNode c => c.Value.ToString(),
            ProcedureConstantNode pc => pc.Procedure.Procedure.Name,
            null => "(null)",
            _ => RenderVar(n.NodeId),
        };
    }
}
