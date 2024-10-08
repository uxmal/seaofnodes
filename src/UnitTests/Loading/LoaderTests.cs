using Reko.Core;
using SeaOfNodes.Nodes;
using System.Runtime.CompilerServices;

namespace SeaOfNodes.UnitTests.Loading
{
    [TestFixture]
    internal class LoaderTests
    {
        private static readonly RegisterStorage r0 = RegisterStorage.Reg32("r0", 0);
        private static readonly RegisterStorage r1 = RegisterStorage.Reg32("r1", 1);

        private void RunTest(
            string sExpected,
            Action<ProcedureBuilder> client,
            [CallerMemberName] string? caller = null)
        {
            var m = new ProcedureBuilder(
                new FakeArchitecture(),
                caller!,
                Address.Ptr32(0x1000));
            client(m);
            var loader = new SeaOfNodes.Loading.Loader();
            var node = loader.Load(m.ToProcedure());
            var sActual = NodePrinter.PrettyPrint(node, 99);
            if (sExpected != sActual)
            {
                Console.WriteLine(sActual);
                Assert.That(sActual, Is.EqualTo(sExpected));
            }
        }

        [Test]
        public void Ldr_Assign()
        {
            var sExp =
            #region Expected
@"
{ id:1, lbl:Start, in:[], out:[2,3,6] }

{ id:6, lbl:#0x2A<32>, in:[1], out:[8] }

{ id:3, lbl:<Entry>, in:[1], out:[5] }

{ id:5, lbl:l00001000, in:[3], out:[4,7] }

{ id:7, lbl:return, in:[5], out:[2] }

{ id:4, lbl:<Exit>, in:[5], out:[2,8] }

{ id:8, lbl:Use_8, in:[4,6], out:[2] }

{ id:2, lbl:Stop, in:[1,7,4,8], out:[] }

";
            #endregion

            RunTest(sExp, m =>
            {
                var r1 = m.Reg(LoaderTests.r1);
                m.Assign(r1, 42);
                m.Return();
            });
        }

        [Test]
        public void Ldr_AssignSum()
        {
            var sExp =
            #region Expected
@"
{ id:1, lbl:Start, in:[], out:[2,3,6,7] }

{ id:7, lbl:def_r1, in:[1], out:[8,11] }
{ id:6, lbl:def_r0, in:[1], out:[8] }
{ id:8, lbl: + , in:[null,6,7], out:[10] }

{ id:3, lbl:<Entry>, in:[1], out:[5] }

{ id:5, lbl:l00001000, in:[3], out:[4,9] }

{ id:9, lbl:return, in:[5], out:[2] }

{ id:4, lbl:<Exit>, in:[5], out:[2,10,11] }

{ id:11, lbl:use_r1, in:[4,7], out:[2] }
{ id:10, lbl:use_r0, in:[4,8], out:[2] }

{ id:2, lbl:Stop, in:[1,9,4,10,11], out:[] }

";
            #endregion

            RunTest(sExp, m =>
            {
                var r0 = m.Reg(LoaderTests.r0);
                var r1 = m.Reg(LoaderTests.r1);
                m.Assign(r0, m.IAdd(r0, r1));
                m.Return();
            });
        }
    }
}
