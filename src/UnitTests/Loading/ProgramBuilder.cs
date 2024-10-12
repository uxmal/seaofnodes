using Reko.Core;
using Reko.Core.Graphs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeaOfNodes.UnitTests.Loading
{
    public class ProgramBuilder
    {
        private readonly IProcessorArchitecture arch;
        private Dictionary<Address, ProcedureBuilder> builders;
        private Address addr;

        public ProgramBuilder(IProcessorArchitecture arch)
        {
            this.arch = arch;
            this.addr = Address.Ptr32(0x1000);
            this.builders = [];
        }

        public void AddProcedure(string name, Action<ProcedureBuilder> client)
        {
            var m = new ProcedureBuilder(arch, name, this.addr);
            client(m);
            builders.Add(addr, m);
            addr += 0x800;
        }

        public TestProgram BuildProgram()
        {
            var procs = new Dictionary<Address, Procedure>();
            foreach (var (a, b) in builders)
            {
                procs.Add(a, b.ToProcedure());
            }
            var symbols = procs.Values.ToDictionary(p => p.Name);
            var callgraph = new CallGraph();
            foreach (var (addr, b) in builders)
            {
                b.Fixup(symbols, callgraph);
            }
            return new TestProgram(procs, callgraph);
        }
    }
}
