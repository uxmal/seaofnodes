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
            var factory = new NodeFactory();
            var loader = new Loader(m.ToProcedure(), factory);
            Node node = loader.Load();
            var x = NodePrinter.PrettyPrint(node, 99);
            node = node.Propagate(factory);
            var sActual = NodePrinter.PrettyPrint(node, 99);
            if (sExpected != sActual)
            {
                Console.WriteLine(x);
                Console.WriteLine("=========");

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
{ id:1, lbl:Start, in:[], out:[2,3,12,7] }

{ id:7, lbl:#0x29<32>, in:[1], out:[11] }
{ id:12, lbl:#0x2A<32>, in:[1], out:[10] }

{ id:3, lbl:<Entry>, in:[1], out:[5] }

{ id:5, lbl:l00001000, in:[3], out:[9] }

{ id:9, lbl:return, in:[5], out:[4] }

{ id:4, lbl:<Exit>, in:[9], out:[2,10,11] }

{ id:11, lbl:use_r1, in:[4,7], out:[2] }
{ id:10, lbl:use_r0, in:[4,12], out:[2] }

{ id:2, lbl:Stop, in:[1,4,10,11], out:[] }

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
