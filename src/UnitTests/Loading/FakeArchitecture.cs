﻿using Reko.Core;
using Reko.Core.Analysis;
using Reko.Core.Assemblers;
using Reko.Core.Code;
using Reko.Core.Emulation;
using Reko.Core.Expressions;
using Reko.Core.Lib;
using Reko.Core.Machine;
using Reko.Core.Memory;
using Reko.Core.Rtl;
using Reko.Core.Types;
using System.Diagnostics.CodeAnalysis;

namespace SeaOfNodes.UnitTests.Loading
{
    public class FakeArchitecture : IProcessorArchitecture
    {
        private readonly RegisterStorage[] gpRegs;
        private readonly RegisterStorage[] lowRegs;
        private readonly RegisterStorage[] hiRegs;
        private readonly Dictionary<string, RegisterStorage> regs;

        public FakeArchitecture() {
            var factory = new StorageFactory();
            gpRegs = factory.RangeOfReg32(32, "r{0}");
            lowRegs = gpRegs.Select(r => new RegisterStorage($"r{r.Number}l", r.Number, 0, PrimitiveType.Word16)).ToArray();
            hiRegs = gpRegs.Select(r => new RegisterStorage($"r{r.Number}h", r.Number, 16, PrimitiveType.Word16)).ToArray();
            regs = gpRegs
                .Concat(lowRegs)
                .Concat(hiRegs)
                .ToDictionary(k => k.Name);
        }

        public FlagGroupStorage? CarryFlag => throw new NotImplementedException();

        public int DefaultBase => throw new NotImplementedException();

        public string? Description { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public EndianServices Endianness => throw new NotImplementedException();

        public RegisterStorage? FpuStackRegister => throw new NotImplementedException();

        public PrimitiveType FramePointerType => throw new NotImplementedException();

        public int InstructionBitSize => throw new NotImplementedException();

        public int CodeMemoryGranularity => throw new NotImplementedException();

        public int MemoryGranularity => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public PrimitiveType PointerType => PrimitiveType.Ptr32;

        public MaskedPattern[] ProcedurePrologs => throw new NotImplementedException();

        public int ReturnAddressOnStack => throw new NotImplementedException();

        public IServiceProvider Services => throw new NotImplementedException();

        public RegisterStorage StackRegister { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public PrimitiveType WordWidth => throw new NotImplementedException();

        public IAssembler CreateAssembler(string? asmDialect)
        {
            throw new NotImplementedException();
        }

        public MemoryArea CreateCodeMemoryArea(Address baseAddress, byte[] bytes)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<MachineInstruction> CreateDisassembler(EndianImageReader imageReader)
        {
            throw new NotImplementedException();
        }

        public IProcessorEmulator CreateEmulator(SegmentMap segmentMap, IPlatformEmulator envEmulator)
        {
            throw new NotImplementedException();
        }

        public T? CreateExtension<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public Expression CreateFpuStackAccess(IStorageBinder binder, int offset, DataType dataType)
        {
            throw new NotImplementedException();
        }

        public Frame CreateFrame()
        {
            return new Frame(this, PrimitiveType.Ptr32);
        }

        public FrameApplicationBuilder CreateFrameApplicationBuilder(IStorageBinder binder, CallSite site)
        {
            throw new NotImplementedException();
        }

        public EndianImageReader CreateImageReader(MemoryArea memoryArea, Address addr)
        {
            throw new NotImplementedException();
        }

        public EndianImageReader CreateImageReader(MemoryArea memoryArea, Address addr, long cbUnits)
        {
            throw new NotImplementedException();
        }

        public EndianImageReader CreateImageReader(MemoryArea memoryArea, long offsetBegin, long offsetEnd)
        {
            throw new NotImplementedException();
        }

        public EndianImageReader CreateImageReader(MemoryArea memoryArea, long offset)
        {
            throw new NotImplementedException();
        }

        public ImageWriter CreateImageWriter()
        {
            throw new NotImplementedException();
        }

        public ImageWriter CreateImageWriter(MemoryArea memoryArea, Address addr)
        {
            throw new NotImplementedException();
        }

        public IEqualityComparer<MachineInstruction>? CreateInstructionComparer(Normalize norm)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Address> CreatePointerScanner(SegmentMap map, EndianImageReader rdr, IEnumerable<Address> knownAddresses, PointerScannerFlags flags)
        {
            throw new NotImplementedException();
        }

        public ProcessorState CreateProcessorState()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<RtlInstructionCluster> CreateRewriter(EndianImageReader rdr, ProcessorState state, IStorageBinder binder, IRewriterHost host)
        {
            throw new NotImplementedException();
        }

        public Expression CreateStackAccess(IStorageBinder binder, int cbOffset, DataType dataType)
        {
            throw new NotImplementedException();
        }

        public CallingConvention? GetCallingConvention(string? ccName)
        {
            throw new NotImplementedException();
        }

        public FlagGroupStorage? GetFlagGroup(RegisterStorage flagRegister, uint grf)
        {
            throw new NotImplementedException();
        }

        public FlagGroupStorage? GetFlagGroup(string name)
        {
            throw new NotImplementedException();
        }

        public FlagGroupStorage[] GetFlags()
        {
            throw new NotImplementedException();
        }

        public SortedList<string, int> GetMnemonicNames()
        {
            throw new NotImplementedException();
        }

        public int? GetMnemonicNumber(string sMnemonic)
        {
            throw new NotImplementedException();
        }

        public RegisterStorage? GetRegister(string name)
        {
            return regs.TryGetValue(name, out var reg)
                ? reg
                : null;
        }

        public RegisterStorage? GetRegister(StorageDomain domain, BitRange range)
        {
            uint i = (uint)domain;
            if (i < gpRegs.Length)
            {
                if (0 <= range.Lsb && range.Lsb < 16)
                {
                    if (range.Msb <= 16)
                        return lowRegs[i];
                    else
                        return gpRegs[i];
                }
                else
                {
                    return hiRegs[i];
                }
            }
            return null;
        }

        public RegisterStorage[] GetRegisters()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<FlagGroupStorage> GetSubFlags(FlagGroupStorage flags)
        {
            throw new NotImplementedException();
        }

        public string GrfToString(RegisterStorage flagRegister, string prefix, uint grf)
        {
            throw new NotImplementedException();
        }

        public List<RtlInstruction>? InlineCall(Address addrCallee, Address addrContinuation, EndianImageReader rdr, IStorageBinder binder)
        {
            throw new NotImplementedException();
        }

        public bool IsStackArgumentOffset(long frameOffset)
        {
            throw new NotImplementedException();
        }

        public void LoadUserOptions(Dictionary<string, object>? options)
        {
            throw new NotImplementedException();
        }

        public Address MakeAddressFromConstant(Constant c, bool codeAlign)
        {
            throw new NotImplementedException();
        }

        public Address MakeSegmentedAddress(Constant seg, Constant offset)
        {
            throw new NotImplementedException();
        }

        public void PostprocessProgram(Program program)
        {
            throw new NotImplementedException();
        }

        public Address? ReadCodeAddress(int size, EndianImageReader rdr, ProcessorState? state)
        {
            throw new NotImplementedException();
        }

        public Constant ReinterpretAsFloat(Constant rawBits)
        {
            throw new NotImplementedException();
        }

        public string RenderInstructionOpcode(MachineInstruction instr, EndianImageReader rdr)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, object>? SaveUserOptions()
        {
            throw new NotImplementedException();
        }

        public bool TryCreateImageReader(IMemory memory, Address addr, [MaybeNullWhen(false)] out EndianImageReader rdr)
        {
            throw new NotImplementedException();
        }

        public bool TryCreateImageReader(IMemory memory, Address addr, long cbUnits, [MaybeNullWhen(false)] out EndianImageReader rdr)
        {
            throw new NotImplementedException();
        }

        public bool TryGetRegister(string name, [MaybeNullWhen(false)] out RegisterStorage reg)
        {
            throw new NotImplementedException();
        }

        public bool TryParseAddress(string? txtAddr, [MaybeNullWhen(false)] out Address addr)
        {
            throw new NotImplementedException();
        }

        public bool TryRead(IMemory mem, Address addr, PrimitiveType dt, [MaybeNullWhen(false)] out Constant value)
        {
            throw new NotImplementedException();
        }

        public bool TryRead(MemoryArea mem, Address addr, PrimitiveType dt, [MaybeNullWhen(false)] out Constant value)
        {
            throw new NotImplementedException();
        }

        public bool TryRead(EndianImageReader rdr, PrimitiveType dt, [MaybeNullWhen(false)] out Constant value)
        {
            throw new NotImplementedException();
        }
    }
}