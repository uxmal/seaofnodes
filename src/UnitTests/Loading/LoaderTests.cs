﻿using Reko.Core;
using Reko.Core.Expressions;
using Reko.Core.Types;
using SeaOfNodes.Loading;
using SeaOfNodes.Nodes;
using SeaOfNodes.UnitTests.Loading;
using System.Runtime.CompilerServices;

namespace SeaOfNodes.UnitTests.Loading
{
    [TestFixture]
    internal class LoaderTests
    {
        private readonly RegisterStorage r0;
        private readonly RegisterStorage r1;
        private readonly RegisterStorage r2;

        private readonly RegisterStorage r0l;

        private readonly FakeArchitecture arch;

        public LoaderTests()
        {
            this.arch = new FakeArchitecture();
            r0 = arch.GetRegister((StorageDomain)0, new(0, 32))!;
            r1 = arch.GetRegister((StorageDomain)1, new(0, 32))!;
            r2 = arch.GetRegister((StorageDomain)2, new(0, 32))!;

            r0l = arch.GetRegister(0, new(0, 16))!;
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
            var loader = new Loader(m.ToProcedure(), new NodeFactory());
            var node = loader.Load();
            var sActual = NodePrinter.PrettyPrint(node, 99);
            if (sExpected != sActual)
            {
                Console.WriteLine(sActual);
                Assert.That(sActual, Is.EqualTo(sExpected));
            }
        }

        private ProcedureConstant External(string name)
        {
            return new ProcedureConstant(
                arch.PointerType,
                new ExternalProcedure(name, new FunctionType()));
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

{ id:5, lbl:l00001000, in:[3], out:[7] }

{ id:7, lbl:return, in:[5], out:[4] }

{ id:4, lbl:<Exit>, in:[7], out:[2,8] }

{ id:8, lbl:use_r1, in:[4,6], out:[2] }

{ id:2, lbl:Stop, in:[1,4,8], out:[] }

";
            #endregion

            RunTest(sExp, m =>
            {
                var r1 = m.Reg(this.r1);
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

{ id:5, lbl:l00001000, in:[3], out:[9] }

{ id:9, lbl:return, in:[5], out:[4] }

{ id:4, lbl:<Exit>, in:[9], out:[2,10,11] }

{ id:11, lbl:use_r1, in:[4,7], out:[2] }
{ id:10, lbl:use_r0, in:[4,8], out:[2] }

{ id:2, lbl:Stop, in:[1,4,10,11], out:[] }

";
            #endregion

            RunTest(sExp, m =>
            {
                var r0 = m.Reg(this.r0);
                var r1 = m.Reg(this.r1);
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

{ id:5, lbl:l00001000, in:[3], out:[11] }

{ id:11, lbl:branch, in:[5,10], out:[12,13] }

{ id:13, lbl:Proj, in:[11], out:[7] }
{ id:12, lbl:Proj, in:[11], out:[6] }

{ id:6, lbl:l00001004, in:[12], out:[7] }

{ id:7, lbl:done, in:[13,6], out:[15,16] }

{ id:16, lbl:phi_16, in:[7,14,8], out:[17] }

{ id:15, lbl:return, in:[7], out:[4] }

{ id:4, lbl:<Exit>, in:[15], out:[2,17] }

{ id:17, lbl:use_r0, in:[4,16], out:[2] }

{ id:2, lbl:Stop, in:[1,4,17], out:[] }

";
            #endregion

            RunTest(sExp, m =>
            {
                var r0 = m.Reg(this.r0);
                var r1 = m.Reg(this.r1);
                m.Branch(m.Ge0(r0), "done");
                m.Assign(r0, m.Neg(r0));
                m.Label("done");
                m.Return();
            });
        }

        [Test]
        public void Ldr_Call()
        {
            var sExpected =
            #region Expected
@"
{ id:1, lbl:Start, in:[], out:[2,3,6,7,9] }

{ id:9, lbl:def_r1, in:[1], out:[10,15] }
{ id:7, lbl:def_r0, in:[1], out:[8] }
{ id:6, lbl:add, in:[1], out:[11] }

{ id:3, lbl:<Entry>, in:[1], out:[5] }

{ id:5, lbl:l00001000, in:[3], out:[8,10,11] }

{ id:10, lbl:use_r1, in:[5,9], out:[11] }
{ id:8, lbl:use_r0, in:[5,7], out:[11] }

{ id:11, lbl:call, in:[5,6,8,10], out:[12,13] }

{ id:12, lbl:def_r0, in:[11], out:[14] }

{ id:13, lbl:return, in:[11], out:[4] }

{ id:4, lbl:<Exit>, in:[13], out:[2,14,15] }

{ id:15, lbl:use_r1, in:[4,9], out:[2] }
{ id:14, lbl:use_r0, in:[4,12], out:[2] }

{ id:2, lbl:Stop, in:[1,4,14,15], out:[] }

";
            #endregion

            RunTest(sExpected, m =>
            {
                var r0 = m.Reg(this.r0);
                var r1 = m.Reg(this.r1);

                m.Call(External("add"))
                    .Use(r0).Use(r1)
                    .Def(r0);
                m.Return();
            });
        }

        [Test]
        public void Ldr_TwoCalls()
        {
            var sExpected =
            #region Expected
@"
{ id:1, lbl:Start, in:[], out:[2,3,6,7,9,13] }

{ id:13, lbl:mul, in:[1], out:[16] }
{ id:9, lbl:def_r1, in:[1], out:[10,15,21] }
{ id:7, lbl:def_r0, in:[1], out:[8] }
{ id:6, lbl:mul, in:[1], out:[11] }

{ id:3, lbl:<Entry>, in:[1], out:[5] }

{ id:5, lbl:l00001000, in:[3], out:[8,10,11] }

{ id:10, lbl:use_r1, in:[5,9], out:[11] }
{ id:8, lbl:use_r0, in:[5,7], out:[11] }

{ id:11, lbl:call, in:[5,6,8,10], out:[12,14,15,16] }

{ id:15, lbl:use_r1, in:[11,9], out:[16] }
{ id:12, lbl:def_r0, in:[11], out:[14,18,22] }
{ id:14, lbl:use_r0, in:[11,12], out:[16] }

{ id:16, lbl:call, in:[11,13,14,15], out:[17,19] }

{ id:17, lbl:def_r0, in:[16], out:[18] }
{ id:18, lbl: + , in:[_,17,12], out:[20] }

{ id:19, lbl:return, in:[16], out:[4] }

{ id:4, lbl:<Exit>, in:[19], out:[2,20,21,22] }

{ id:22, lbl:use_r2, in:[4,12], out:[2] }
{ id:21, lbl:use_r1, in:[4,9], out:[2] }
{ id:20, lbl:use_r0, in:[4,18], out:[2] }

{ id:2, lbl:Stop, in:[1,4,20,21,22], out:[] }

";
            #endregion

            RunTest(sExpected, m =>
            {
                var r0 = m.Reg(this.r0);
                var r1 = m.Reg(this.r1);
                var r2 = m.Reg(this.r2);
                m.Call(External("mul"))
                    .Use(r0).Use(r1)
                    .Def(r0);
                m.Assign(r2, r0);
                m.Call(External("mul"))
                    .Use(r0).Use(r1)
                    .Def(r0);
                m.Assign(r0, m.IAdd(r0, r2));
                m.Return();
            });
        }

        [Test]
        public void Ldr_Slice()
        {
            var sExpected =
            #region Expected
@"
{ id:1, lbl:Start, in:[], out:[2,3,6,7] }

{ id:7, lbl:def_Mem, in:[1], out:[8,12] }
{ id:6, lbl:def_r0, in:[1], out:[8,10] }
{ id:8, lbl:Mem, in:[7,6], out:[10] }
{ id:10, lbl:SEQ, in:[_,6,8], out:[11] }

{ id:3, lbl:<Entry>, in:[1], out:[5] }

{ id:5, lbl:l00001000, in:[3], out:[9] }

{ id:9, lbl:return, in:[5], out:[4] }

{ id:4, lbl:<Exit>, in:[9], out:[2,11,12] }

{ id:12, lbl:use_Mem, in:[4,7], out:[2] }
{ id:11, lbl:use_r0, in:[4,10], out:[2] }

{ id:2, lbl:Stop, in:[1,4,11,12], out:[] }

";
            #endregion

            RunTest(sExpected, m =>
            {
                var r0 = m.Reg(this.r0);
                var r0l = m.Reg(this.r0l);

                m.Assign(r0l, m.Mem16(r0));
                m.Return();
            });
        }

        [Test]
        public void Ldr_Loop()
        {
            var sExpected =
            #region Expected
@"
{ id:1, lbl:Start, in:[], out:[2,3,9,10,12,20] }

{ id:20, lbl:#1<32>, in:[1], out:[21] }
{ id:12, lbl:#0xA<32>, in:[1], out:[13] }
{ id:10, lbl:#0<32>, in:[1], out:[18] }
{ id:9, lbl:#0<32>, in:[1], out:[11] }

{ id:3, lbl:<Entry>, in:[1], out:[5] }

{ id:5, lbl:l00001000, in:[3], out:[7] }

{ id:7, lbl:head, in:[5,6], out:[11,14,18] }

{ id:11, lbl:phi_11, in:[7,21,9], out:[13,19,21,22] }
{ id:21, lbl: + , in:[_,11,20], out:[11] }
{ id:19, lbl: + , in:[_,18,11], out:[18] }
{ id:18, lbl:phi_18, in:[7,19,10], out:[19,23] }
{ id:13, lbl: <= , in:[_,11,12], out:[14] }

{ id:14, lbl:branch, in:[7,13], out:[15,16] }

{ id:16, lbl:Proj, in:[14], out:[6] }

{ id:6, lbl:body, in:[16], out:[7] }

{ id:15, lbl:Proj, in:[14], out:[8] }

{ id:8, lbl:l00001014, in:[15], out:[17] }

{ id:17, lbl:return, in:[8], out:[4] }

{ id:4, lbl:<Exit>, in:[17], out:[2,22,23] }

{ id:23, lbl:use_r0, in:[4,18], out:[2] }
{ id:22, lbl:use_r1, in:[4,11], out:[2] }

{ id:2, lbl:Stop, in:[1,4,22,23], out:[] }

";
            #endregion

            RunTest(sExpected, m =>
            {
                var r0 = m.Reg(this.r0);
                var r1 = m.Reg(this.r1);

                m.Assign(r1, 0);
                m.Assign(r0, 0);
                m.Goto("head");

                m.Label("body");
                m.Assign(r0, m.IAdd(r0, r1));
                m.Assign(r1, m.IAdd(r1, 1));

                m.Label("head");
                m.Branch(m.Le(r1, 10), "body");
                m.Return();
            });
        }
    }
}
