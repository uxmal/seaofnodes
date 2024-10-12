using Reko.Core;
using Reko.Core.Expressions;
using Reko.Core.Types;
using SeaOfNodes.Loading;
using SeaOfNodes.Nodes;
using System.Runtime.CompilerServices;
using System.Text;

namespace SeaOfNodes.UnitTests.Loading;

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
        var proc = m.ToProcedure();
        var loader = new Loader(proc, new NodeFactory(proc));
        var node = loader.Load();
        var sActual = NodePrinter.PrettyPrint(node, 99);
        if (sExpected != sActual)
        {
            Console.WriteLine(sActual);
            Assert.That(sActual, Is.EqualTo(sExpected));
        }
    }

    private void RunProgramTest(
        string sExpected,
        Action<ProgramBuilder> client,
        [CallerMemberName] string? caller = null)
    {
        var m = new ProgramBuilder(arch);
        client(m);
        var program = m.BuildProgram();
        var sb = new StringBuilder();
        sb.AppendLine();
        foreach (var proc in program.Procedures.Values)
        {
            sb.AppendLine($"== {proc.Name} ======");
            var loader = new Loader(proc, new NodeFactory(proc));
            var node = loader.Load();
            sb = NodePrinter.PrettyPrint(node, 99, sb);
        }
        var sActual = sb.ToString();
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

{ id:13, lbl:11.true, in:[11], out:[7] }

{ id:12, lbl:11.false, in:[11], out:[6] }

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
{ id:1, lbl:Start, in:[], out:[2,3,6,7,12] }

{ id:6, lbl:def_r0, in:[1], out:[7,9] }
{ id:9, lbl:slice, in:[_,6], out:[10] }
{ id:7, lbl:Mem, in:[1,6], out:[10] }
{ id:10, lbl:SEQ, in:[_,9,7], out:[11] }

{ id:3, lbl:<Entry>, in:[1], out:[5] }

{ id:5, lbl:l00001000, in:[3], out:[8] }

{ id:8, lbl:return, in:[5], out:[4] }

{ id:4, lbl:<Exit>, in:[8], out:[2,11,12] }

{ id:12, lbl:use_Mem, in:[4,1], out:[2] }
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

{ id:16, lbl:14.true, in:[14], out:[6] }

{ id:6, lbl:body, in:[16], out:[7] }

{ id:15, lbl:14.false, in:[14], out:[8] }

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

    [Test]
    public void Ldr_Store()
    {
        string sExpected =
        #region Expected
@"
{ id:1, lbl:Start, in:[], out:[2,3,6,7,8,12] }

{ id:12, lbl:#4<32>, in:[1], out:[13] }
{ id:8, lbl:#3<16>, in:[1], out:[9] }
{ id:6, lbl:def_r0, in:[1], out:[7] }
{ id:7, lbl:Mem, in:[1,6], out:[9,13,18] }
{ id:13, lbl: + , in:[_,7,12], out:[14] }

{ id:3, lbl:<Entry>, in:[1], out:[5] }

{ id:5, lbl:l00001000, in:[3], out:[9] }

{ id:9, lbl:Store, in:[5,7,8], out:[10,11] }
{ id:11, lbl:9.mem, in:[9], out:[14,17] }
{ id:14, lbl:Mem, in:[11,13], out:[16] }
{ id:10, lbl:9.ctrl, in:[9], out:[15] }

{ id:15, lbl:return, in:[10], out:[4] }

{ id:4, lbl:<Exit>, in:[15], out:[2,16,17,18] }

{ id:18, lbl:use_r1, in:[4,7], out:[2] }
{ id:17, lbl:use_Mem, in:[4,11], out:[2] }
{ id:16, lbl:use_r0, in:[4,14], out:[2] }

{ id:2, lbl:Stop, in:[1,4,16,17,18], out:[] }

";

        #endregion
        RunTest(sExpected, m =>
        {
            var r0 = m.Reg(this.r0);
            var r1 = m.Reg(this.r1);

            m.Assign(r1, m.Mem32(r0));
            m.MStore(r1, m.Word16(3));
            m.Assign(r0, m.Mem32(m.IAdd(r1, 4)));
            m.Return();
        });
    }

    [Test]
    public void Ldr_Swap()
    {
        string sExpected =
        #region Expected
@"
{ id:1, lbl:Start, in:[], out:[2,3,6,7,8,10,14] }

{ id:14, lbl:#4<i32>, in:[1], out:[15] }
{ id:8, lbl:#4<i32>, in:[1], out:[9] }
{ id:6, lbl:def_r0, in:[1], out:[7,9,11,15,20] }
{ id:15, lbl: + , in:[_,6,14], out:[16] }
{ id:9, lbl: + , in:[_,6,8], out:[10] }
{ id:10, lbl:Mem, in:[1,9], out:[11,23] }
{ id:7, lbl:Mem, in:[1,6], out:[16,22] }

{ id:3, lbl:<Entry>, in:[1], out:[5] }

{ id:5, lbl:l00001000, in:[3], out:[11] }

{ id:11, lbl:Store, in:[5,6,10], out:[12,13] }
{ id:12, lbl:11.ctrl, in:[11], out:[16] }

{ id:16, lbl:Store, in:[12,15,7], out:[17,18] }
{ id:18, lbl:16.mem, in:[16], out:[21] }
{ id:17, lbl:16.ctrl, in:[16], out:[19] }

{ id:19, lbl:return, in:[17], out:[4] }

{ id:4, lbl:<Exit>, in:[19], out:[2,20,21,22,23] }

{ id:23, lbl:use_r2, in:[4,10], out:[2] }
{ id:22, lbl:use_r1, in:[4,7], out:[2] }
{ id:21, lbl:use_Mem, in:[4,18], out:[2] }
{ id:20, lbl:use_r0, in:[4,6], out:[2] }

{ id:2, lbl:Stop, in:[1,4,20,21,22,23], out:[] }

";
        #endregion

        RunTest(sExpected, m =>
        {
            var r0 = m.Reg(this.r0);
            var r1 = m.Reg(this.r1);
            var r2 = m.Reg(this.r2);

            m.Assign(r1, m.Mem32(r0));
            m.Assign(r2, m.Mem32(r0, 4));
            m.MStore(r0, r2);
            m.MStore(m.IAddS(r0, 4), r1);
            m.Return();
        });
    }

    [Test]
    public void Ldr_MemStackAccess()
    {
        string sExpected =
        #region Expected
@"
{ id:1, lbl:Start, in:[], out:[2,3,6,7,9,13,18] }

{ id:18, lbl:#4<32>, in:[1], out:[19] }
{ id:13, lbl:#4<32>, in:[1], out:[14] }
{ id:9, lbl:def_r1, in:[1], out:[10,16] }
{ id:7, lbl:#4<32>, in:[1], out:[8] }
{ id:6, lbl:def_fp, in:[1], out:[8,21] }
{ id:8, lbl: - , in:[_,6,7], out:[10,14,17,19] }
{ id:19, lbl: + , in:[_,8,18], out:[22] }
{ id:14, lbl: + , in:[_,8,13], out:[15] }

{ id:3, lbl:<Entry>, in:[1], out:[5] }

{ id:5, lbl:l00001000, in:[3], out:[10] }

{ id:10, lbl:Store, in:[5,8,9], out:[11,12] }
{ id:12, lbl:10.mem, in:[10], out:[15,17,24] }
{ id:17, lbl:Mem, in:[12,8], out:[23] }
{ id:15, lbl:Mem, in:[12,14], out:[16] }
{ id:16, lbl: * , in:[_,9,15], out:[25] }
{ id:11, lbl:10.ctrl, in:[10], out:[20] }

{ id:20, lbl:return, in:[11], out:[4] }

{ id:4, lbl:<Exit>, in:[20], out:[2,21,22,23,24,25] }

{ id:25, lbl:use_r0, in:[4,16], out:[2] }
{ id:24, lbl:use_Mem, in:[4,12], out:[2] }
{ id:23, lbl:use_r1, in:[4,17], out:[2] }
{ id:22, lbl:use_r2, in:[4,19], out:[2] }
{ id:21, lbl:use_fp, in:[4,6], out:[2] }

{ id:2, lbl:Stop, in:[1,4,21,22,23,24,25], out:[] }

";
        #endregion
        RunTest(sExpected, m =>
        {
            var r0 = m.Reg(this.r0);
            var r1 = m.Reg(this.r1);
            var sp = m.Reg(this.r2);

            var fp = m.FramePointer;
            m.Assign(sp, fp);
            m.Assign(sp, m.ISub(sp, 4));
            m.MStore(sp, r1);

            m.Assign(r0, m.IMul(r1, m.Mem32(m.IAdd(sp, 4))));

            m.Assign(r1, m.Mem32(sp));
            m.Assign(sp, m.IAdd(sp, 4));
            m.Return();
        });
    }


    [Test]
    public void Ldr_FactorialReg()
    {
        string sExpected =
        #region Expected
@"
== factorial ======

{ id:1, lbl:Start, in:[], out:[2,3,8,9,14,16,17,22,24,28,35,38] }

{ id:38, lbl:def_r1, in:[1], out:[37] }
{ id:28, lbl:#4<32>, in:[1], out:[29] }
{ id:24, lbl:factorial, in:[1], out:[25] }
{ id:22, lbl:#1<32>, in:[1], out:[23] }
{ id:17, lbl:#4<32>, in:[1], out:[18] }
{ id:16, lbl:def_r2, in:[1], out:[18,33] }
{ id:18, lbl: - , in:[_,16,17], out:[19,26,29] }
{ id:29, lbl: + , in:[_,18,28], out:[33] }
{ id:14, lbl:#1<32>, in:[1], out:[31] }
{ id:9, lbl:#0<32>, in:[1], out:[10] }
{ id:8, lbl:def_r0, in:[1], out:[10,19,23] }
{ id:23, lbl: - , in:[_,8,22], out:[27] }
{ id:10, lbl: > , in:[_,8,9], out:[11] }

{ id:3, lbl:<Entry>, in:[1], out:[5] }

{ id:5, lbl:l00001000, in:[3], out:[11] }

{ id:11, lbl:branch, in:[5,10], out:[12,13] }

{ id:13, lbl:11.true, in:[11], out:[7] }

{ id:7, lbl:recurse, in:[13], out:[19] }

{ id:19, lbl:Store, in:[7,18,8], out:[20,21] }
{ id:21, lbl:19.mem, in:[19], out:[26,35] }
{ id:26, lbl:Mem, in:[21,18], out:[27,37] }
{ id:27, lbl: *s , in:[_,26,23], out:[31] }
{ id:20, lbl:19.ctrl, in:[19], out:[25] }

{ id:25, lbl:call, in:[20,24], out:[30] }

{ id:30, lbl:return, in:[25], out:[4] }

{ id:12, lbl:11.false, in:[11], out:[6] }

{ id:6, lbl:base, in:[12], out:[15] }

{ id:15, lbl:return, in:[6], out:[4] }

{ id:4, lbl:<Exit>, in:[15,30], out:[2,31,32,33,34,35,36,37,39] }

{ id:37, lbl:phi_37, in:[4,38,26], out:[39] }
{ id:39, lbl:use_r1, in:[4,37], out:[2] }
{ id:35, lbl:phi_35, in:[4,1,21], out:[36] }
{ id:36, lbl:use_Mem, in:[4,35], out:[2] }
{ id:33, lbl:phi_33, in:[4,16,29], out:[34] }
{ id:34, lbl:use_r2, in:[4,33], out:[2] }
{ id:31, lbl:phi_31, in:[4,14,27], out:[32] }
{ id:32, lbl:use_r0, in:[4,31], out:[2] }

{ id:2, lbl:Stop, in:[1,4,32,34,36,39], out:[] }

";

        #endregion
        RunProgramTest(sExpected, p =>
        {
            p.AddProcedure("factorial", m =>
            {
                var r0 = m.Reg(this.r0);
                var r1 = m.Reg(this.r1);
                var sp = m.Reg(this.r2);

                m.Branch(m.Gt0(r0), "recurse");

                m.Label("base");
                m.Assign(r0, 1);
                m.Return();

                m.Label("recurse");
                m.Assign(sp, m.ISub(sp, 4));
                m.MStore(sp, r0);
                m.Assign(r0, m.ISub(r0, 1));
                m.Call("factorial");
                m.Assign(r1, m.Mem32(sp));
                m.Assign(r0, m.SMul(r1, r0));
                m.Assign(sp, m.IAdd(sp, 4));

                m.Return();
            });
        });
    }
}
