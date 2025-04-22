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

        _ticks += instruction switch
        {
            Instruction.Nop x => Nop(x),
            Instruction.LoadImm16 x => LoadImm16(x),
            _ => throw new NotImplementedException($"instruction {instruction} not implemented in cpu"),
        };
    }

    private byte FetchByte() => _bus.Read(_registers.ProgramCounter++);

    private ushort FetchUShort()
    {
        var low = FetchByte();
        var high = FetchByte();
        return BinaryUtil.ToUShort(high, low);
    }

    private ulong Nop(Instruction.Nop inst) => 1;

    private ulong LoadImm16(Instruction.LoadImm16 inst)
    {
        var value = FetchUShort();

        if (inst.Destination == Instruction.Register16.BC) _registers.BC = value;
        if (inst.Destination == Instruction.Register16.DE) _registers.DE = value;
        if (inst.Destination == Instruction.Register16.HL) _registers.HL = value;
        if (inst.Destination == Instruction.Register16.StackPointer) _registers.StackPointer = value;

        return 3;
    }
}
