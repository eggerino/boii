using System;
using System.Security.Cryptography.X509Certificates;
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

        Instruction.IncrementRegister8 x => IncrementRegister8(x),
        Instruction.DecrementRegister8 x => DecrementRegister8(x),

        Instruction.IncrementRegister16 x => IncrementRegister16(x),
        Instruction.DecrementRegister16 x => DecrementRegister16(x),
        Instruction.AddRegister16ToHL x => AddRegister16ToHL(x),

        Instruction.RotateLeftA x => RotateLeftA(x),
        Instruction.RotateRightA x => RotateRightA(x),
        Instruction.RotateLeftCarryA x => RotateLeftCarryA(x),
        Instruction.RotateRightCarryA x => RotateRightCarryA(x),

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

    private ulong IncrementRegister8(Instruction.IncrementRegister8 inst)
    {
        byte newValue = inst.Operand switch
        {
            Instruction.Register8.B => ++_registers.B,
            Instruction.Register8.C => ++_registers.C,
            Instruction.Register8.D => ++_registers.D,
            Instruction.Register8.E => ++_registers.E,
            Instruction.Register8.H => ++_registers.H,
            Instruction.Register8.L => ++_registers.L,
            Instruction.Register8.A => ++_registers.A,
            _ => 0,
        };

        if (inst.Operand == Instruction.Register8.HLAsPointer)
        {
            newValue = (byte)(_bus.Read(_registers.HL) + 1);
            _bus.Write(_registers.HL, newValue);
        }

        if (newValue == 0) _registers.Zero = true;
        _registers.Subtraction = false;
        if (newValue == 0x10) _registers.HalfCarry = true;

        return inst.Operand == Instruction.Register8.HLAsPointer ? 3ul : 1ul;
    }

    private ulong DecrementRegister8(Instruction.DecrementRegister8 inst)
    {
        byte oldValue = inst.Operand switch
        {
            Instruction.Register8.B => _registers.B--,
            Instruction.Register8.C => _registers.C--,
            Instruction.Register8.D => _registers.D--,
            Instruction.Register8.E => _registers.E--,
            Instruction.Register8.H => _registers.H--,
            Instruction.Register8.L => _registers.L--,
            Instruction.Register8.A => _registers.A--,
            _ => 0,
        };

        if (inst.Operand == Instruction.Register8.HLAsPointer)
        {
            oldValue = _bus.Read(_registers.HL);
            _bus.Write(_registers.HL, (byte)(oldValue - 1));
        }

        if (oldValue == 1) _registers.Zero = true;
        _registers.Subtraction = true;
        if ((oldValue & 0b0000_1111) == 0) _registers.HalfCarry = true;

        return inst.Operand == Instruction.Register8.HLAsPointer ? 3ul : 1ul;
    }

    private ulong IncrementRegister16(Instruction.IncrementRegister16 inst)
    {
        if (inst.Operand == Instruction.Register16.BC) _registers.BC++;
        if (inst.Operand == Instruction.Register16.DE) _registers.DE++;
        if (inst.Operand == Instruction.Register16.HL) _registers.HL++;
        if (inst.Operand == Instruction.Register16.StackPointer) _registers.StackPointer++;

        return 2;
    }

    private ulong DecrementRegister16(Instruction.DecrementRegister16 inst)
    {
        if (inst.Operand == Instruction.Register16.BC) _registers.BC--;
        if (inst.Operand == Instruction.Register16.DE) _registers.DE--;
        if (inst.Operand == Instruction.Register16.HL) _registers.HL--;
        if (inst.Operand == Instruction.Register16.StackPointer) _registers.StackPointer--;

        return 2;
    }

    private ulong AddRegister16ToHL(Instruction.AddRegister16ToHL inst)
    {
        int oldValue = _registers.HL;
        int newValue = oldValue + inst.Operand switch
        {
            Instruction.Register16.BC => _registers.BC,
            Instruction.Register16.DE => _registers.DE,
            Instruction.Register16.HL => _registers.HL,
            Instruction.Register16.StackPointer => _registers.StackPointer,
            _ => 0,
        };

        _registers.Subtraction = false;
        if (oldValue <= 0x0FFF && 0x0FFF < newValue) _registers.HalfCarry = true;
        if (oldValue <= 0xFFFF && 0xFFFF < newValue) _registers.Carry = true;
        _registers.HL = (ushort)newValue;

        return 2;
    }

    private ulong RotateLeftA(Instruction.RotateLeftA _)
    {
        byte a = _registers.A;
        var carry = a > 0b0111_1111;
        a <<= 1;
        if (carry) a |= 0b0000_0001;

        _registers.A = a;
        _registers.Zero = false;
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = carry;

        return 1;
    }

    private ulong RotateRightA(Instruction.RotateRightA _)
    {
        byte a = _registers.A;
        var carry = (a % 2) == 1;
        a >>= 1;
        if (carry) a |= 0b1000_0000;

        _registers.A = a;
        _registers.Zero = false;
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = carry;

        return 1;
    }

    private ulong RotateLeftCarryA(Instruction.RotateLeftCarryA _)
    {
        byte a = _registers.A;
        var carry = a > 0b0111_1111;
        a <<= 1;
        if (_registers.Carry) a |= 0b0000_0001;

        _registers.A = a;
        _registers.Zero = false;
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = carry;

        return 1;
    }

    private ulong RotateRightCarryA(Instruction.RotateRightCarryA _)
    {
        byte a = _registers.A;
        var carry = (a % 2) == 1;
        a >>= 1;
        if (_registers.Carry) a |= 0b1000_0000;

        _registers.A = a;
        _registers.Zero = false;
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = carry;

        return 1;
    }

}
