using Reko.Core;
using Reko.Core.Expressions;
using SeaOfNodes.Analyses;
using SeaOfNodes.Loading;
using SeaOfNodes.Nodes;
using SeaOfNodes.UnitTests.Loading;
using System.Runtime.CompilerServices;

namespace SeaOfNodes.UnitTests.Analyses
{
    [TestFixture]
    public class NodeValuePropagatorTests
    {
        private readonly FakeArchitecture arch;

        public NodeValuePropagatorTests()
        {
            this.arch = new FakeArchitecture();
        }

        private void RunTest(
            string sExpected,
            Action<ProcedureBuilder> client,
            [CallerMemberName] string? caller = null)
        {
            var m = new ProcedureBuilder(
                arch,
                caller!,
                Address.Ptr32(0x1000));
            client(m);
            var proc = m.ToProcedure();
            var factory = new NodeFactory(proc);
            var loader = new Loader(proc, factory);
            Node node = loader.Load();
            var x = NodePrinter.PrettyPrintSsa(node, 99);
            node = node.Propagate(factory);
            var sActual = NodePrinter.PrettyPrintSsa(node, 99);
            if (sExpected != sActual)
            {
                //Console.WriteLine(x);
                //Console.WriteLine("=========");

                Console.WriteLine(sActual);
                Assert.That(sActual, Is.EqualTo(sExpected));
            }
        }


        private Identifier Reg(ProcedureBuilder m, string regName)
        {
            return m.Reg(arch.GetRegister(regName)!);
        }

        [Test]
        public void Peep_ConstantFold_IAdd()
        {
            string sExpected =
            #region Expected
@"
def proc():

Peep_ConstantFold_IAdd_entry:
l00001000:
    return
Peep_ConstantFold_IAdd_exit:
    use r1:0x29<32>
    use r0:0x2A<32>

";
            #endregion

            RunTest(sExpected, m =>
            {
                var r0 = Reg(m, "r0");
                var r1 = Reg(m, "r1");

                m.Assign(r0, 1);
                m.Assign(r1, 41);
                m.Assign(r0, m.IAdd(r0, r1));
                m.Return();
            });
        }
    }
}
