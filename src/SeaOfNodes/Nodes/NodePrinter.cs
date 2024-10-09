using SeaOfNodes.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaOfNodes.Nodes
{
    public class NodePrinter
    {
        public static void WriteLine(Node n, StringBuilder sb)
        {
            sb.AppendFormat("{0} id:{1}, lbl:{2}", "{",  n.NodeId, PrintLabel(n.label()));
            if (n.InNodes is null)
            {
                sb.AppendLine("DEAD }");
                return;
            }
            sb.Append(", in:[");
            var sep = "";
            foreach (Node? def in n.InNodes)
            {
                sb.Append(sep);
                sb.Append(def is null ? "_" : $"{def.NodeId}");
                sep = ",";
            }
            sb.Append("]");

            sb.Append(", out:[");
            sep = "";
            foreach (Node use in n.OutNodes)
            {
                sb.Append(sep);
                sb.Append(use is null ? "_" : $"{use.NodeId}");
                sep = ",";
            }
            sb.Append("] ");
            if (n.Type != null) sb.Append(n.Type.str());
            sb.AppendLine("}");
        }


        // Print a node on 1 line, columnar aligned, as:
        // NNID NNAME DDEF DDEF  [[  UUSE UUSE  ]]  TYPE
        // 1234 sssss 1234 1234 1234 1234 1234 1234 tttttt
        public static void WriteLineFixedColumns(Node n, StringBuilder sb)
        {
            sb.AppendFormat("{0,4} {1,-13} ", n.NodeId, PrintLabel(n.label()));
            if (n.InNodes == null)
            {
                sb.AppendLine("DEAD");
                return;
            }
            foreach (Node? def in n.InNodes.Take(4))
                sb.Append(def is null ? "____ " : $"{def.NodeId,4} ");
            for (int i = n.InNodes.Count; i < 4; i++)
                sb.Append("     ");
            sb.Append(" [[  ");
            foreach (Node use in n.OutNodes)
                sb.Append(use is null ? "____ " : $"{use.NodeId,4} ");
            int lim = 6 - Math.Max(n.InNodes.Count, 4);
            for (int i = n.OutNodes.Count; i < lim; i++)
                sb.Append("     ");
            sb.Append(" ]]  ");
            if (n.Type != null) sb.Append(n.Type.str());
            sb.AppendLine();
        }

        private static string PrintLabel(string v)
        {
            if (v.Length > 12)
                return v.Remove(12);
            return v;
        }

        private static StringBuilder NodeId(StringBuilder sb, Node n)
        {
            sb.Append($"%{n.NodeId}");
           /* if (n is ProjNode proj)
            {
                sb.Append(".").Append(proj._idx);
            }
           */
            return sb;
        }

        // Print a node on 1 line, format is inspired by LLVM
        // %id: TYPE = NODE(inputs ....)
        // Nodes as referred to as %id
        public static void WriteLineLlvmFormat(Node n, StringBuilder sb)
        {
            NodeId(sb, n).Append(": ");
            if (n.InNodes is null)
            {
                sb.AppendLine("DEAD");
                return;
            }
            n.Type?.typeName(sb);
            sb.Append(" = ").Append(n.label()).Append("(");
            for (int i = 0; i < n.InNodes.Count; i++)
            {
                Node? def = n.InNodes[i];
                if (i > 0)
                    sb.Append(", ");
                if (def is null)
                    sb.Append('_');
                else NodeId(sb, def);
            }
            sb.AppendLine(")");
        }

        public static void WriteLine(Node n, StringBuilder sb, bool llvmFormat)
        {
            if (llvmFormat) WriteLineLlvmFormat(n, sb);
            else WriteLine(n, sb);
            //else WriteLineFixedColumns(n, sb);
        }

        public static string PrettyPrint(Node node, int depth)
        {
            return /*Parser.SCHEDULED
                ? PrettyPrintScheduled(node, depth, false)
                : */PrettyPrint(node, depth, false);
        }

        // Another bulk pretty-printer.  Makes more effort at basic-block grouping.
        public static string PrettyPrint(Node node, int depth, bool llvmFormat)
        {
            // First, a Breadth First Search at a fixed depth.
            BFS bfs = new(node, depth);
            // Convert just that set to a post-order
            List<Node> rpos = [];
            BitSet visit = new();
            for (int i = bfs._lim; i < bfs._bfs.Count; i++)
                PostOrder(bfs._bfs[i], rpos, visit, bfs._bs);
            // Reverse the post-order walk
            StringBuilder sb = new();
            bool gap = false;
            for (int i = rpos.Count - 1; i >= 0; i--)
            {
                Node n = rpos[i];
                if (n.IsCFG() || n.IsMultiHead())
                {
                    if (!gap)
                        sb.AppendLine(); // Blank before multihead
                    WriteLine(n, sb, llvmFormat); // Print head
                    while (--i >= 0)
                    {
                        Node t = rpos[i];
                        if (!t.IsMultiTail()) { i++; break; }
                        WriteLine(t, sb, llvmFormat);
                    }
                    sb.AppendLine(); // Blank after multitail
                    gap = true;
                }
                else
                {
                    WriteLine(n, sb, llvmFormat);
                    gap = false;
                }
            }
            return sb.ToString();
        }

        private static void PostOrder(Node n, List<Node> rpos, BitSet visit, BitSet bfs)
        {
            if (!bfs.get(n.NodeId))
                return;  // Not in the BFS visit
            if (visit.get(n.NodeId))
                return; // Already post-order walked
            visit.set(n.NodeId);
            // First walk the CFG, then everything
            if (n.IsCFG())
            {
                foreach (Node use in n.OutNodes)
                    if (use is not null && use.IsCFG() && use.OutNodes.Count >= 1)
                        PostOrder(use, rpos, visit, bfs);
                foreach (Node use in n.OutNodes)
                    if (use is not null && use.IsCFG())
                        PostOrder(use, rpos, visit, bfs);
            }
            foreach (Node use in n.OutNodes)
                if (use is not null)
                    PostOrder(use, rpos, visit, bfs);
            // Post-order
            rpos.Add(n);
        }

        // Breadth-first search, broken out in a class to keep in more independent.
        // Maintains a root-set of Nodes at the limit (or past by 1 if MultiHead).
        public class BFS
        {
            // A breadth first search, plus MultiHeads for any MultiTails
            public readonly List<Node> _bfs;
            public readonly BitSet _bs; // Visited members by node id
            public readonly int _depth; // Depth limit
            public readonly int _lim; // From here to _bfs._len can be roots for a reverse search

            public BFS(Node @base, int d)
            {
                _depth = d;
                _bfs = [];
                _bs = new BitSet();

                Add(@base);                // Prime the pump
                int idx = 0, lim = 1;          // Limit is where depth Counter changes
                while (idx < _bfs.Count)
                { // Ran out of nodes below depth
                    Node n = _bfs[idx++];
                    foreach (Node? def in n.InNodes)
                        if (def != null && !_bs.get(def.NodeId))
                            Add(def);
                    if (idx == lim)
                    {    // Depth Counter changes at limit
                        if (--d < 0)
                            break;      // Ran out of depth
                        lim = _bfs.Count;  // New depth limit
                    }
                }
                // Toss things past the limit except multi-heads
                while (idx < _bfs.Count)
                {
                    Node n = _bfs[idx];
                   // if (n.isMultiHead()) idx++;
                   // else
                        Del(idx);
                }

                // Root set is any node with no inputs in the visited set
                lim = _bfs.Count;
                for (int i = _bfs.Count - 1; i >= 0; i--)
                    if (!AnyVisited(_bfs[i]))
                        Swap(i, --lim);
                _lim = lim;
            }
            private void Swap(int x, int y)
            {
                if (x == y) return;
                Node tx = _bfs[x];
                Node ty = _bfs[y];
                _bfs[x] = ty;
                _bfs[y] = tx;
            }

            private void Add(Node n)
            {
                _bfs.Add(n);
                _bs.set(n.NodeId);
            }

            private void Del(int idx)
            {
                Node? n = Utils.del(_bfs, idx);
                _bs.clear(n.NodeId);
            }

            private bool AnyVisited(Node n)
            {
                foreach (Node? def in n.InNodes)
                    if (def is not null && _bs.get(def.NodeId))
                        return true;
                return false;
            }
        }

        // Bulk pretty printer, knowing scheduling information is available
        public static string PrettyPrintScheduled(Node node, int depth, bool llvmFormat)
        {
            // Backwards DFS walk to depth.
            Dictionary<Node, int> ds = [];
            Walk(ds, node, depth);
            // Print by block with least idepth
            StringBuilder sb = new();
            List<Node> bns = [];
            while (ds.Count > 0)
            {
                CFNode? blk = null;
                foreach (Node n in ds.Keys)
                {
                    CFNode? cfg = n is CFNode cfg0 && cfg0.blockHead() 
                        ? cfg0 
                        : (CFNode)n.InNodes[0]!;
                    if (blk is null || cfg.idepth() < blk.idepth())
                        blk = cfg;
                }
                Debug.Assert(blk != null);
                ds.Remove(blk);

                // Print block header
                sb.Append($"{Label(blk) + ":",-13}");
                sb.Append(new string(' ', 20)).Append(" [[  ");
                /*                if (blk is RegionNode || blk is StopNode)
                                    for (int i = (blk is StopNode ? 0 : 1); i < blk.nIns(); i++)
                                        label(sb, blk.cfg(i));

                                else 
                */
                if (blk is not StartNode)
                    Label(sb, blk.Cfg(0));
                sb.AppendLine(" ]]  ");

                // Collect block contents that are in the depth limit
                bns.Clear();
                int xd = int.MaxValue;
                foreach (Node use in blk.OutNodes)
                {
                    if (ds.TryGetValue(use, out int i) &&
                        !(use is CFNode cfg && cfg.blockHead()))
                    {
                        bns.Add(use);
                        xd = Math.Min(xd, i);
                    }
                }
                // Print Phis up front, if any
                for (int i = 0; i < bns.Count; i++)
                    if (bns[i] is PhiNode phi)
                        PrintLine(phi, sb, llvmFormat, bns, i--, ds);

                // Print block contents in depth order, bumping depth until whole block printed
                for (; bns.Count > 0; xd++)
                    for (int i = 0; i < bns.Count; i++)
                        if (ds[bns[i]] == xd)
                            PrintLine(bns[i], sb, llvmFormat, bns, i--, ds);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static void Walk(Dictionary<Node, int> ds, Node node, int d)
        {
            if (ds.TryGetValue(node, out var nd) && d <= nd)
                return;     // Been there, done that
            ds[node] = d;
            if (d == 0)
                return;     // Depth cutoff
            foreach (Node? def in node.InNodes)
                if (def is not null)
                    Walk(ds, def, d - 1);
        }

        static string Label(CFNode blk)
        {
            if (blk is StartNode) return "START";
            return "L" + blk.NodeId;
        }

        static void Label(StringBuilder sb, CFNode blk)
        {
            if (!blk.blockHead()) blk = blk.Cfg(0);
            sb.Append($"{Label(blk),-9} ");
        }

        static void PrintLine(Node n, StringBuilder sb, bool llvmFormat, List<Node> bns, int i, Dictionary<Node, int> ds)
        {
            WriteLine(n, sb, llvmFormat);
            Utils.del(bns, i);
            ds.Remove(n);
        }
    }
}
