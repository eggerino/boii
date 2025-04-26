using System;
using System.Linq;
using System.Runtime.CompilerServices;
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

    public static Cpu CreateWithRegisterState(IGenericIO bus, RegisterDump registerDump)
    {
        var cpu = new Cpu(bus);

        cpu._registers.AF = registerDump.AF;
        cpu._registers.BC = registerDump.BC;
        cpu._registers.DE = registerDump.DE;
        cpu._registers.HL = registerDump.HL;
        cpu._registers.StackPointer = registerDump.StackPointer;
        cpu._registers.ProgramCounter = registerDump.ProgramCounter;

        return cpu;
    }

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

    private byte GetRegister8(Instruction.Register8 register) => register switch
    {
        Instruction.Register8.B => _registers.B,
        Instruction.Register8.C => _registers.C,
        Instruction.Register8.D => _registers.D,
        Instruction.Register8.E => _registers.E,
        Instruction.Register8.H => _registers.H,
        Instruction.Register8.L => _registers.L,
        Instruction.Register8.HLAsPointer => _bus.Read(_registers.HL),
        Instruction.Register8.A => _registers.A,
        _ => 0,
    };

    private void SetRegister8(Instruction.Register8 register, byte value)
    {
        if (register == Instruction.Register8.B) _registers.B = value;
        else if (register == Instruction.Register8.C) _registers.C = value;
        else if (register == Instruction.Register8.D) _registers.D = value;
        else if (register == Instruction.Register8.E) _registers.E = value;
        else if (register == Instruction.Register8.H) _registers.H = value;
        else if (register == Instruction.Register8.L) _registers.L = value;
        else if (register == Instruction.Register8.HLAsPointer) _bus.Write(_registers.HL, value);
        else if (register == Instruction.Register8.A) _registers.A = value;
    }

    private ushort GetRegister16(Instruction.Register16 register) => register switch
    {
        Instruction.Register16.BC => _registers.BC,
        Instruction.Register16.DE => _registers.DE,
        Instruction.Register16.HL => _registers.HL,
        Instruction.Register16.StackPointer => _registers.StackPointer,
        _ => 0,
    };

    private void SetRegister16(Instruction.Register16 register, ushort value)
    {
        if (register == Instruction.Register16.BC) _registers.BC = value;
        else if (register == Instruction.Register16.DE) _registers.DE = value;
        else if (register == Instruction.Register16.HL) _registers.HL = value;
        else if (register == Instruction.Register16.StackPointer) _registers.StackPointer = value;
    }

    private byte ReadFromRegister16Memory(Instruction.Register16Memory register) => register switch
    {
        Instruction.Register16Memory.BC => _bus.Read(_registers.BC),
        Instruction.Register16Memory.DE => _bus.Read(_registers.DE),
        Instruction.Register16Memory.HLInc => _bus.Read(_registers.HL++),
        Instruction.Register16Memory.HLDec => _bus.Read(_registers.HL--),
        _ => 0,
    };

    private void WriteToRegister16Memory(Instruction.Register16Memory register, byte value)
    {
        if (register == Instruction.Register16Memory.BC) _bus.Write(_registers.BC, value);
        else if (register == Instruction.Register16Memory.DE) _bus.Write(_registers.DE, value);
        else if (register == Instruction.Register16Memory.HLInc) _bus.Write(_registers.HL++, value);
        else if (register == Instruction.Register16Memory.HLDec) _bus.Write(_registers.HL--, value);
    }

    private bool GetCondition(Instruction.JumpCondition condition) => condition switch
    {
        Instruction.JumpCondition.NotZero => !_registers.Zero,
        Instruction.JumpCondition.Zero => _registers.Zero,
        Instruction.JumpCondition.NotCarry => !_registers.Carry,
        Instruction.JumpCondition.Carry => _registers.Carry,
        _ => false,
    };

    private bool IsOverflowBit3(int oldValue, int newValue) => oldValue <= 0x000F && newValue > 0x000F;

    private bool IsOverflowBit7(int oldValue, int newValue) => oldValue <= 0x00FF && newValue > 0x00FF;

    private bool IsOverflowBit11(int oldValue, int newValue) => oldValue <= 0x0FFF && newValue > 0x0FFF;

    private bool IsOverflowBit15(int oldValue, int newValue) => oldValue <= 0xFFFF && newValue > 0xFFFF;

    private bool IsBorrowBit4(int oldValue, int decrement) => (oldValue & 0xF) < (decrement & 0xF);

    private ulong Execute(Instruction inst) => inst switch
    {
        Instruction.Nop x => Nop(x),
        Instruction.Stop x => Stop(x),
        Instruction.Halt x => Halt(x),

        Instruction.LoadImm8 x => LoadImm8(x),
        Instruction.LoadRegister8ToRegister8 x => LoadRegister8ToRegister8(x),
        Instruction.LoadImm16 x => LoadImm16(x),
        Instruction.LoadFromA x => LoadFromA(x),
        Instruction.LoadIntoA x => LoadIntoA(x),
        Instruction.LoadFromStackPointer x => LoadFromStackPointer(x),

        Instruction.IncrementRegister8 x => IncrementRegister8(x),
        Instruction.DecrementRegister8 x => DecrementRegister8(x),

        Instruction.AddToA x => AddToA(x),
        Instruction.AddToAImm8 x => AddToAImm8(x),
        Instruction.AddToACarry x => AddToACarry(x),
        Instruction.AddToAImm8Carry x => AddToAImm8Carry(x),
        Instruction.SubtractToA x => SubtractToA(x),
        Instruction.SubtractToAImm8 x => SubtractToAImm8(x),
        Instruction.SubtractToACarry x => SubtractToACarry(x),
        Instruction.SubtractToAImm8Carry x => SubtractToAImm8Carry(x),
        Instruction.CompareToA x => CompareToA(x),
        Instruction.CompareToAImm8 x => CompareToAImm8(x),

        Instruction.IncrementRegister16 x => IncrementRegister16(x),
        Instruction.DecrementRegister16 x => DecrementRegister16(x),
        Instruction.AddRegister16ToHL x => AddRegister16ToHL(x),

        Instruction.RotateLeftA x => RotateLeftA(x),
        Instruction.RotateRightA x => RotateRightA(x),
        Instruction.RotateLeftCarryA x => RotateLeftCarryA(x),
        Instruction.RotateRightCarryA x => RotateRightCarryA(x),

        Instruction.DecimalAdjustAccumulator x => DecimalAdjustAccumulator(x),
        Instruction.ComplementAccumulator x => ComplementAccumulator(x),

        Instruction.SetCarryFlag x => SetCarryFlag(x),
        Instruction.ComplementCarryFlag x => ComplementCarryFlag(x),

        Instruction.AndToA x => AndToA(x),
        Instruction.AndToAImm8 x => AndToAImm8(x),
        Instruction.XorToA x => XorToA(x),
        Instruction.XorToAImm8 x => XorToAImm8(x),
        Instruction.OrToA x => OrToA(x),
        Instruction.OrToAImm8 x => OrToAImm8(x),

        Instruction.JumpRelative x => JumpRelative(x),
        Instruction.ConditionalJumpRelative x => ConditionalJumpRelative(x),
        Instruction.Jump x => Jump(x),
        Instruction.ConditionalJump x => ConditionalJump(x),
        Instruction.JumpHL x => JumpHL(x),

        Instruction.Call x => Call(x),
        Instruction.ConditionalCall x => ConditionalCall(x),
        Instruction.Restart x => Restart(x),

        _ => throw new NotImplementedException($"instruction {inst} not implemented in cpu"),
    };

    private ulong Nop(Instruction.Nop _) => 1;

    private ulong Stop(Instruction.Stop _)
    {
        throw new NotImplementedException("[TODO] Stop is currently not supported");
    }

    private ulong Halt(Instruction.Halt _)
    {
        throw new NotImplementedException("[TODO] Halt is currently not supported");
    }

    private ulong LoadImm8(Instruction.LoadImm8 inst)
    {
        var value = FetchByte();
        SetRegister8(inst.Destination, value);
        return inst.Destination == Instruction.Register8.HLAsPointer ? 3ul : 2ul;
    }

    private ulong LoadRegister8ToRegister8(Instruction.LoadRegister8ToRegister8 inst)
    {
        var value = GetRegister8(inst.Source);
        SetRegister8(inst.Destination, value);
        return Enumerable.Any([inst.Source, inst.Destination], x => x == Instruction.Register8.HLAsPointer)
            ? 2ul
            : 1;
    }

    private ulong LoadImm16(Instruction.LoadImm16 inst)
    {
        var value = FetchUShort();
        SetRegister16(inst.Destination, value);
        return 3;
    }

    private ulong LoadFromA(Instruction.LoadFromA inst)
    {
        var value = _registers.A;
        WriteToRegister16Memory(inst.Destination, value);
        return 2;
    }

    private ulong LoadIntoA(Instruction.LoadIntoA inst)
    {
        var value = ReadFromRegister16Memory(inst.Source);
        _registers.A = value;
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
        int value = GetRegister8(inst.Operand);
        value++;
        SetRegister8(inst.Operand, (byte)value);

        if ((byte)value == 0) _registers.Zero = true;
        _registers.Subtraction = false;
        if (IsOverflowBit3(value - 1, value)) _registers.HalfCarry = true;

        return inst.Operand == Instruction.Register8.HLAsPointer ? 3ul : 1ul;
    }

    private ulong DecrementRegister8(Instruction.DecrementRegister8 inst)
    {
        int value = GetRegister8(inst.Operand);
        value--;
        SetRegister8(inst.Operand, (byte)value);

        if ((byte)value == 0) _registers.Zero = true;
        _registers.Subtraction = true;
        if (IsBorrowBit4(value + 1, 1)) _registers.HalfCarry = true;

        return inst.Operand == Instruction.Register8.HLAsPointer ? 3ul : 1ul;
    }

    private ulong AddToA(Instruction.AddToA inst)
    {
        var oldValue = _registers.A;
        var operand = GetRegister8(inst.Operand);
        var newValue = oldValue + operand;

        _registers.A = (byte)newValue;
        if ((byte)newValue == 0) _registers.Zero = true;
        _registers.Subtraction = false;
        if (IsOverflowBit3(oldValue, newValue)) _registers.HalfCarry = true;
        if (IsOverflowBit7(oldValue, newValue)) _registers.Carry = true;

        return inst.Operand == Instruction.Register8.HLAsPointer ? 2ul : 1;
    }

    private ulong AddToAImm8(Instruction.AddToAImm8 _)
    {
        var operand = FetchByte();
        var oldValue = _registers.A;
        var newValue = oldValue + operand;

        _registers.A = (byte)newValue;
        if ((byte)newValue == 0) _registers.Zero = true;
        _registers.Subtraction = false;
        if (IsOverflowBit3(oldValue, newValue)) _registers.HalfCarry = true;
        if (IsOverflowBit7(oldValue, newValue)) _registers.Carry = true;

        return 2;
    }

    private ulong AddToACarry(Instruction.AddToACarry inst)
    {
        var oldValue = _registers.A;
        var operand = GetRegister8(inst.Operand);
        var newValue = oldValue + operand;
        if (_registers.Carry) newValue++;

        _registers.A = (byte)newValue;
        if ((byte)newValue == 0) _registers.Zero = true;
        _registers.Subtraction = false;
        if (IsOverflowBit3(oldValue, newValue)) _registers.HalfCarry = true;
        if (IsOverflowBit7(oldValue, newValue)) _registers.Carry = true;

        return inst.Operand == Instruction.Register8.HLAsPointer ? 2ul : 1;
    }

    private ulong AddToAImm8Carry(Instruction.AddToAImm8Carry _)
    {
        var operand = FetchByte();
        var oldValue = _registers.A;
        var newValue = oldValue + operand;
        if (_registers.Carry) newValue++;

        _registers.A = (byte)newValue;
        if ((byte)newValue == 0) _registers.Zero = true;
        _registers.Subtraction = false;
        if (IsOverflowBit3(oldValue, newValue)) _registers.HalfCarry = true;
        if (IsOverflowBit7(oldValue, newValue)) _registers.Carry = true;

        return 2;
    }

    private ulong SubtractToA(Instruction.SubtractToA inst)
    {
        var oldValue = _registers.A;
        var operand = GetRegister8(inst.Operand);
        var newValue = oldValue - operand;

        _registers.A = (byte)newValue;
        if ((byte)newValue == 0) _registers.Zero = true;
        _registers.Subtraction = true;
        if (IsBorrowBit4(oldValue, operand)) _registers.HalfCarry = true;
        if (operand > oldValue) _registers.Carry = true;

        return inst.Operand == Instruction.Register8.HLAsPointer ? 2ul : 1;
    }

    private ulong SubtractToAImm8(Instruction.SubtractToAImm8 _)
    {
        var operand = FetchByte();
        var oldValue = _registers.A;

        var newValue = oldValue - operand;

        _registers.A = (byte)newValue;
        if ((byte)newValue == 0) _registers.Zero = true;
        _registers.Subtraction = true;
        if (IsBorrowBit4(oldValue, operand)) _registers.HalfCarry = true;
        if (operand > oldValue) _registers.Carry = true;

        return 2;
    }

    private ulong SubtractToACarry(Instruction.SubtractToACarry inst)
    {
        var oldValue = _registers.A;
        int operand = GetRegister8(inst.Operand);
        if (_registers.Carry) operand++;
        var newValue = oldValue - operand;

        _registers.A = (byte)newValue;
        if ((byte)newValue == 0) _registers.Zero = true;
        _registers.Subtraction = true;
        if (IsBorrowBit4(oldValue, operand)) _registers.HalfCarry = true;
        if (operand > oldValue) _registers.Carry = true;

        return inst.Operand == Instruction.Register8.HLAsPointer ? 2ul : 1;
    }

    private ulong SubtractToAImm8Carry(Instruction.SubtractToAImm8Carry _)
    {
        int operand = FetchByte();
        var oldValue = _registers.A;
        if (_registers.Carry) operand++;
        var newValue = oldValue - operand;

        _registers.A = (byte)newValue;
        if ((byte)newValue == 0) _registers.Zero = true;
        _registers.Subtraction = true;
        if (IsBorrowBit4(oldValue, operand)) _registers.HalfCarry = true;
        if (operand > oldValue) _registers.Carry = true;

        return 2;
    }

    private ulong CompareToA(Instruction.CompareToA inst)
    {
        var a = _registers.A;
        var operand = GetRegister8(inst.Operand);

        if (a == operand) _registers.Zero = true;
        _registers.Subtraction = true;
        if ((operand & 0b0000_1111) > (a & 0b0000_1111)) _registers.HalfCarry = true;
        if (operand > a) _registers.Carry = true;

        return inst.Operand == Instruction.Register8.HLAsPointer ? 2ul : 1;
    }

    private ulong CompareToAImm8(Instruction.CompareToAImm8 _)
    {
        var a = _registers.A;
        var operand = FetchByte();

        if (a == operand) _registers.Zero = true;
        _registers.Subtraction = true;
        if ((operand & 0b0000_1111) > (a & 0b0000_1111)) _registers.HalfCarry = true;
        if (operand > a) _registers.Carry = true;

        return 2;
    }

    private ulong IncrementRegister16(Instruction.IncrementRegister16 inst)
    {
        var value = GetRegister16(inst.Operand);
        value++;
        SetRegister16(inst.Operand, value);
        return 2;
    }

    private ulong DecrementRegister16(Instruction.DecrementRegister16 inst)
    {
        var value = GetRegister16(inst.Operand);
        value--;
        SetRegister16(inst.Operand, value);
        return 2;
    }

    private ulong AddRegister16ToHL(Instruction.AddRegister16ToHL inst)
    {
        var oldValue = _registers.HL;
        var operand = GetRegister16(inst.Operand);
        var newValue = oldValue + operand;

        _registers.HL = (ushort)newValue;
        _registers.Subtraction = false;
        if (IsOverflowBit11(oldValue, newValue)) _registers.HalfCarry = true;
        if (IsOverflowBit15(oldValue, newValue)) _registers.Carry = true;

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

    private ulong DecimalAdjustAccumulator(Instruction.DecimalAdjustAccumulator _)
    {
        var a = _registers.A;
        var carry = false;

        if (_registers.Subtraction)
        {
            if (_registers.HalfCarry) a -= 0x06;
            if (_registers.Carry) a -= 0x60;
        }
        else
        {
            if (_registers.HalfCarry || (a & 0x0F) > 0x09) a += 0x06;
            if (_registers.Carry || a > 0x99)
            {
                a += 0x60;
                carry = true;
            }
        }

        _registers.A = a;
        if (a == 0) _registers.Zero = true;
        _registers.HalfCarry = false;
        _registers.Carry = carry;

        return 1;
    }

    private ulong ComplementAccumulator(Instruction.ComplementAccumulator _)
    {
        _registers.A = (byte)~_registers.A;
        _registers.Subtraction = true;
        _registers.HalfCarry = true;

        return 1;
    }

    private ulong SetCarryFlag(Instruction.SetCarryFlag _)
    {
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = true;

        return 1;
    }

    private ulong ComplementCarryFlag(Instruction.ComplementCarryFlag _)
    {
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = !_registers.Carry;

        return 1;
    }

    private ulong AndToA(Instruction.AndToA inst)
    {
        var operand = GetRegister8(inst.Operand);

        _registers.A &= operand;
        if (_registers.A == 0) _registers.Zero = true;
        _registers.Subtraction = false;
        _registers.HalfCarry = true;
        _registers.Carry = false;

        return inst.Operand == Instruction.Register8.HLAsPointer ? 2ul : 1;
    }

    private ulong AndToAImm8(Instruction.AndToAImm8 _)
    {
        byte operand = FetchByte();

        _registers.A &= operand;
        if (_registers.A == 0) _registers.Zero = true;
        _registers.Subtraction = false;
        _registers.HalfCarry = true;
        _registers.Carry = false;

        return 2;
    }

    private ulong XorToA(Instruction.XorToA inst)
    {
        var operand = GetRegister8(inst.Operand);

        _registers.A ^= operand;
        if (_registers.A == 0) _registers.Zero = true;
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = false;

        return inst.Operand == Instruction.Register8.HLAsPointer ? 2ul : 1;
    }

    private ulong XorToAImm8(Instruction.XorToAImm8 _)
    {
        byte operand = FetchByte();

        _registers.A ^= operand;
        if (_registers.A == 0) _registers.Zero = true;
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = false;

        return 2;
    }

    private ulong OrToA(Instruction.OrToA inst)
    {
        var operand = GetRegister8(inst.Operand);

        _registers.A |= operand;
        if (_registers.A == 0) _registers.Zero = true;
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = false;

        return inst.Operand == Instruction.Register8.HLAsPointer ? 2ul : 1;
    }

    private ulong OrToAImm8(Instruction.OrToAImm8 _)
    {
        byte operand = FetchByte();

        _registers.A |= operand;
        if (_registers.A == 0) _registers.Zero = true;
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = false;

        return 2;
    }

    private ulong JumpRelative(Instruction.JumpRelative _)
    {
        var offset = (sbyte)FetchByte();
        _registers.ProgramCounter = (ushort)(_registers.ProgramCounter + offset);

        return 3;
    }

    private ulong ConditionalJumpRelative(Instruction.ConditionalJumpRelative inst)
    {
        var condition = GetCondition(inst.Condition);
        var offset = (sbyte)FetchByte();

        if (condition)
        {
            _registers.ProgramCounter = (ushort)(_registers.ProgramCounter + offset);
        }

        return condition ? 3ul : 2;
    }

    private ulong Jump(Instruction.Jump _)
    {
        _registers.ProgramCounter = FetchUShort();
        return 4;
    }

    private ulong ConditionalJump(Instruction.ConditionalJump inst)
    {
        var condition = GetCondition(inst.Condition);
        var target = FetchUShort();

        if (condition) _registers.ProgramCounter = target;

        return condition ? 4ul : 3;
    }

    private ulong JumpHL(Instruction.JumpHL _)
    {
        _registers.ProgramCounter = _registers.HL;
        return 1;
    }

    private ulong Call(Instruction.Call _)
    {
        var address = FetchUShort();

        var returnAddress = _registers.ProgramCounter;
        var (high, low) = BinaryUtil.ToBytes(returnAddress);
        _bus.Write(--_registers.StackPointer, high);
        _bus.Write(--_registers.StackPointer, low);

        _registers.ProgramCounter = address;

        return 6;
    }

    private ulong ConditionalCall(Instruction.ConditionalCall inst)
    {
        var address = FetchUShort();
        var condition = GetCondition(inst.Condition);

        if (!condition)
        {
            return 3;
        }

        var returnAddress = _registers.ProgramCounter;
        var (high, low) = BinaryUtil.ToBytes(returnAddress);
        _bus.Write(--_registers.StackPointer, high);
        _bus.Write(--_registers.StackPointer, low);

        _registers.ProgramCounter = address;

        return 6;
    }

    private ulong Restart(Instruction.Restart inst)
    {
        var address = (byte)(inst.Target * 8);

        var returnAddress = _registers.ProgramCounter;
        var (high, low) = BinaryUtil.ToBytes(returnAddress);
        _bus.Write(--_registers.StackPointer, high);
        _bus.Write(--_registers.StackPointer, low);

        _registers.ProgramCounter = address;

        return 4;
    }
}
