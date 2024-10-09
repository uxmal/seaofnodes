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

{ id:5, lbl:l00001000, in:[3], out:[7,4] }

{ id:4, lbl:<Exit>, in:[5], out:[2,8] }

{ id:8, lbl:use_r1, in:[4,6], out:[2] }

{ id:2, lbl:Stop, in:[1,4,8], out:[] }

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
{ id:8, lbl: + , in:[_,6,7], out:[10] }

{ id:3, lbl:<Entry>, in:[1], out:[5] }

{ id:5, lbl:l00001000, in:[3], out:[9,4] }

{ id:4, lbl:<Exit>, in:[5], out:[2,10,11] }

{ id:11, lbl:use_r1, in:[4,7], out:[2] }
{ id:10, lbl:use_r0, in:[4,8], out:[2] }

{ id:2, lbl:Stop, in:[1,4,10,11], out:[] }

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

        [Test]
        public void Ldr_Branch()
        {
            var sExp =
            #region Expected
@"
{ id:1, lbl:Start, in:[], out:[2,3,8,9] }

{ id:9, lbl:#0<32>, in:[1], out:[10] }
{ id:8, lbl:def_r0, in:[1], out:[10,14,16] }
{ id:14, lbl:-, in:[_,8], out:[16] }
{ id:10, lbl: >= , in:[_,8,9], out:[11] }

{ id:3, lbl:<Entry>, in:[1], out:[5] }

{ id:5, lbl:l00001000, in:[3], out:[11,6,7] }

{ id:11, lbl:branch, in:[5,10], out:[12,13] }

{ id:13, lbl:Proj, in:[11], out:[7] }
{ id:12, lbl:Proj, in:[11], out:[6] }

{ id:6, lbl:l00001004, in:[12,5], out:[7] }

{ id:7, lbl:done, in:[13,5,6], out:[15,4,16] }

{ id:16, lbl:phi_16, in:[7,14,8], out:[17] }

{ id:4, lbl:<Exit>, in:[7], out:[2,17] }

{ id:17, lbl:use_r0, in:[4,16], out:[2] }

{ id:2, lbl:Stop, in:[1,4,17], out:[] }

";
            #endregion

            RunTest(sExp, m =>
            {
                var r0 = m.Reg(LoaderTests.r0);
                var r1 = m.Reg(LoaderTests.r1);
                m.Branch(m.Ge0(r0), "done");
                m.Assign(r0, m.Neg(r0));
                m.Label("done");
                m.Return();
            });
        }

    }
}
