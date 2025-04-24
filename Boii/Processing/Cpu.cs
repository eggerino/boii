using System;
using Boii.Abstractions;
using Boii.Errors;
using Boii.Util;

namespace Boii.Processing;

public class Cpu
{
    public record RegisterDump(ushort AF, ushort BC, ushort DE, ushort HL, ushort StackPointer, ushort ProgramCounter);

    private readonly CpuRegisters _registers = CpuRegisters.Create();
    private ulong _ticks = 0;
    private readonly IGenericIO _bus;

    private Cpu(IGenericIO bus) => _bus = bus;

    public static Cpu Create(IGenericIO bus) => new(bus);

    public ulong Ticks => _ticks;

    public RegisterDump GetRegisterDump() => new(
        AF: _registers.AF,
        BC: _registers.BC,
        DE: _registers.DE,
        HL: _registers.HL,
        StackPointer: _registers.StackPointer,
        ProgramCounter: _registers.ProgramCounter);

    public void Step()
    {
        var opcode = FetchByte();

        if (Instruction.FromOpcode(opcode) is not Instruction instruction)
            throw InvalidOpcode.Create(opcode, (ushort)(_registers.ProgramCounter - 1));

        _ticks += Execute(instruction);
    }

    private byte FetchByte() => _bus.Read(_registers.ProgramCounter++);

    private ushort FetchUShort()
    {
        var low = FetchByte();
        var high = FetchByte();
        return BinaryUtil.ToUShort(high, low);
    }

    private ulong Execute(Instruction inst) => inst switch
        {
            Instruction.Nop x => Nop(x),

            Instruction.LoadImm8 x => LoadImm8(x),
            Instruction.LoadImm16 x => LoadImm16(x),
            Instruction.LoadFromA x => LoadFromA(x),
            Instruction.LoadIntoA x => LoadIntoA(x),
            Instruction.LoadFromStackPointer x => LoadFromStackPointer(x),

            _ => throw new NotImplementedException($"instruction {inst} not implemented in cpu"),
        };

    private ulong Nop(Instruction.Nop _) => 1;

    private ulong LoadImm8(Instruction.LoadImm8 inst)
    {
        var value = FetchByte();

        if (inst.Destination == Instruction.Register8.B) _registers.B = value;
        if (inst.Destination == Instruction.Register8.C) _registers.C = value;
        if (inst.Destination == Instruction.Register8.D) _registers.D = value;
        if (inst.Destination == Instruction.Register8.E) _registers.E = value;
        if (inst.Destination == Instruction.Register8.H) _registers.H = value;
        if (inst.Destination == Instruction.Register8.L) _registers.L = value;
        if (inst.Destination == Instruction.Register8.HLAsPointer) _bus.Write(_registers.HL, value);
        if (inst.Destination == Instruction.Register8.A) _registers.A = value;

        return inst.Destination == Instruction.Register8.HLAsPointer ? 3ul : 2ul;
    }

    private ulong LoadImm16(Instruction.LoadImm16 inst)
    {
        var value = FetchUShort();

        if (inst.Destination == Instruction.Register16.BC) _registers.BC = value;
        if (inst.Destination == Instruction.Register16.DE) _registers.DE = value;
        if (inst.Destination == Instruction.Register16.HL) _registers.HL = value;
        if (inst.Destination == Instruction.Register16.StackPointer) _registers.StackPointer = value;

        return 3;
    }

    private ulong LoadFromA(Instruction.LoadFromA inst)
    {
        var value = _registers.A;

        if (inst.Destination == Instruction.Register16Memory.BC) _bus.Write(_registers.BC, value);
        if (inst.Destination == Instruction.Register16Memory.DE) _bus.Write(_registers.DE, value);
        if (inst.Destination == Instruction.Register16Memory.HLInc) _bus.Write(_registers.HL++, value);
        if (inst.Destination == Instruction.Register16Memory.HLDec) _bus.Write(_registers.HL--, value);

        return 2;
    }

    private ulong LoadIntoA(Instruction.LoadIntoA inst)
    {
        if (inst.Source == Instruction.Register16Memory.BC) _registers.A = _bus.Read(_registers.BC);
        if (inst.Source == Instruction.Register16Memory.DE) _registers.A = _bus.Read(_registers.DE);
        if (inst.Source == Instruction.Register16Memory.HLInc) _registers.A = _bus.Read(_registers.HL++);
        if (inst.Source == Instruction.Register16Memory.HLDec) _registers.A = _bus.Read(_registers.HL--);

        return 2;
    }

    private ulong LoadFromStackPointer(Instruction.LoadFromStackPointer _)
    {
        var destination = FetchUShort();

        _bus.Write(destination++, (byte)_registers.StackPointer);
        _bus.Write(destination, (byte)(_registers.StackPointer >> 8));

        return 5;
    }
}
