using Reko.Core;
using Reko.Core.Graphs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaOfNodes.UnitTests.Loading
{
    public class TestProgram
    {
        public TestProgram(IDictionary<Address, Procedure> procs, CallGraph callgraph)
        {
            Procedures = new(procs);
            Callgraph = callgraph;
        }

        public SortedList<Address, Procedure> Procedures { get; }
        public CallGraph Callgraph { get; }
    }
}
