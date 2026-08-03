using System;
using System.Linq;
using Boii.Abstractions;
using Boii.Errors;
using Boii.Processing.Instructions;
using Boii.Util;

namespace Boii.Processing;

public class Cpu
{   
    // Stack manipulation
    private ulong Push(Instruction.Push inst)
    {
        var value = GetRegister16Stack(inst.Register);
        var (high, low) = BinaryUtil.ToBytes(value);
        _bus.Write(--_registers.StackPointer, high);
        _bus.Write(--_registers.StackPointer, low);
        return 4;
    }

    private ulong Pop(Instruction.Pop inst)
    {
        var low = _bus.Read(_registers.StackPointer++);
        var high = _bus.Read(_registers.StackPointer++);
        var value = BinaryUtil.ToUShort(high, low);
        SetRegister16Stack(inst.Register, value);
        return 3;
    }

    private ulong AddSignedLiteral8ToStackPointer(Instruction.AddSignedLiteral8ToStackPointer _)
    {
        var oldValue = _registers.StackPointer;
        var operand = (sbyte)FetchByte();
        var newValue = oldValue + operand;

        _registers.StackPointer = (ushort)newValue;
        _registers.Zero = false;
        _registers.Subtraction = false;
        _registers.HalfCarry = IsOverflowBit3(oldValue, operand);
        _registers.Carry = IsOverflowBit7(oldValue, operand);

        return 4;
    }

    private ulong LoadFromStackPointerIntoLiteral16Pointer(Instruction.LoadFromStackPointerIntoLiteral16Pointer _)
    {
        var destination = FetchUShort();

        _bus.Write(destination++, (byte)_registers.StackPointer);
        _bus.Write(destination, (byte)(_registers.StackPointer >> 8));

        return 5;
    }

    private ulong LoadFromStackPointerPlusSignedLiteral8IntoHL(Instruction.LoadFromStackPointerPlusSignedLiteral8IntoHL _)
    {
        var oldValue = _registers.StackPointer;
        var operand = (sbyte)FetchByte();
        var newValue = oldValue + operand;

        _registers.HL = (ushort)newValue;
        _registers.Zero = false;
        _registers.Subtraction = false;
        _registers.HalfCarry = IsOverflowBit3(oldValue, operand);
        _registers.Carry = IsOverflowBit7(oldValue, operand);

        return 3;
    }

    private ulong LoadFromHLIntoStackPointer(Instruction.LoadFromHLIntoStackPointer _)
    {
        _registers.StackPointer = _registers.HL;
        return 2;
    }

    // 16 Bit instructions
    private ulong Prefixed(Instruction.Prefixed _)
    {
        var nextOpcode = FetchByte();

        var inst = PrefixedInstruction.FromOpcode(nextOpcode);

        return ExecutePrefixed(inst);
    }

    private ulong ExecutePrefixed(PrefixedInstruction inst) => inst switch
    {
        // Bit shift
        PrefixedInstruction.RotateLeft x => PrefixedRotateLeft(x),
        PrefixedInstruction.RotateLeftThroughCarry x => PrefixedRotateLeftThroughCarry(x),
        PrefixedInstruction.RotateRight x => PrefixedRotateRight(x),
        PrefixedInstruction.RotateRightThroughCarry x => PrefixedRotateRightThroughCarry(x),
        PrefixedInstruction.ShiftLeftArithmetic x => PrefixedShiftLeftArithmetic(x),
        PrefixedInstruction.ShiftRightArithmetic x => PrefixedShiftRightArithmetic(x),
        PrefixedInstruction.Swap x => PrefixedSwap(x),
        PrefixedInstruction.ShiftRightLogical x => PrefixedShiftRightLogical(x),

        // Bit flag
        PrefixedInstruction.CheckBit x => PrefixedCheckBit(x),
        PrefixedInstruction.SetBit x => PrefixedSetBit(x),
        PrefixedInstruction.ResetBit x => PrefixedResetBit(x),
        _ => throw PatternMatchingError.Create(inst),
    };

    // Bit shift
    private ulong PrefixedRotateLeft(PrefixedInstruction.RotateLeft inst)
    {
        var operand = GetRegister8(inst.Operand);
        var carry = operand > 0b0111_1111;
        operand <<= 1;
        if (carry) operand |= 0b0000_0001;

        SetRegister8(inst.Operand, operand);
        _registers.Zero = operand == 0;
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = carry;

        return inst.Operand == Register8.HLAsPointer ? 4ul : 2;
    }

    private ulong PrefixedRotateLeftThroughCarry(PrefixedInstruction.RotateLeftThroughCarry inst)
    {
        var operand = GetRegister8(inst.Operand);
        var carry = operand > 0b0111_1111;
        operand <<= 1;
        if (_registers.Carry) operand |= 0b0000_0001;

        SetRegister8(inst.Operand, operand);
        _registers.Zero = operand == 0;
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = carry;

        return inst.Operand == Register8.HLAsPointer ? 4ul : 2;
    }

    private ulong PrefixedRotateRight(PrefixedInstruction.RotateRight inst)
    {
        var operand = GetRegister8(inst.Operand);
        var carry = (operand % 2) == 1;
        operand >>= 1;
        if (carry) operand |= 0b1000_0000;

        SetRegister8(inst.Operand, operand);
        _registers.Zero = operand == 0;
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = carry;

        return inst.Operand == Register8.HLAsPointer ? 4ul : 2;
    }

    private ulong PrefixedRotateRightThroughCarry(PrefixedInstruction.RotateRightThroughCarry inst)
    {
        var operand = GetRegister8(inst.Operand);
        var carry = (operand % 2) == 1;
        operand >>= 1;
        if (_registers.Carry) operand |= 0b1000_0000;

        SetRegister8(inst.Operand, operand);
        _registers.Zero = operand == 0;
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = carry;

        return inst.Operand == Register8.HLAsPointer ? 4ul : 2;
    }

    private ulong PrefixedShiftLeftArithmetic(PrefixedInstruction.ShiftLeftArithmetic inst)
    {
        var operand = GetRegister8(inst.Operand);
        var carry = operand > 0b0111_1111;
        operand <<= 1;

        SetRegister8(inst.Operand, operand);
        _registers.Zero = operand == 0;
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = carry;

        return inst.Operand == Register8.HLAsPointer ? 4ul : 2;
    }

    private ulong PrefixedShiftRightArithmetic(PrefixedInstruction.ShiftRightArithmetic inst)
    {
        var operand = GetRegister8(inst.Operand);
        var highBit = operand > 0b0111_1111;
        var carry = (operand % 2) == 1;
        operand >>= 1;
        if (highBit) operand |= 0b1000_0000;

        SetRegister8(inst.Operand, operand);
        _registers.Zero = operand == 0;
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = carry;

        return inst.Operand == Register8.HLAsPointer ? 4ul : 2;
    }

    private ulong PrefixedSwap(PrefixedInstruction.Swap inst)
    {
        var operand = GetRegister8(inst.Operand);
        var lowerNibble = operand & 0xF;
        operand >>= 4;
        operand |= (byte)(lowerNibble << 4);

        SetRegister8(inst.Operand, operand);
        _registers.Zero = operand == 0;
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = false;

        return inst.Operand == Register8.HLAsPointer ? 4ul : 2;
    }

    private ulong PrefixedShiftRightLogical(PrefixedInstruction.ShiftRightLogical inst)
    {
        var operand = GetRegister8(inst.Operand);
        var carry = (operand % 2) == 1;
        operand >>= 1;

        SetRegister8(inst.Operand, operand);
        _registers.Zero = operand == 0;
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = carry;

        return inst.Operand == Register8.HLAsPointer ? 4ul : 2;
    }

    // Bit flag
    private ulong PrefixedCheckBit(PrefixedInstruction.CheckBit inst)
    {
        var operand = GetRegister8(inst.Operand);
        var index = inst.Index.ToInt();

        _registers.Zero = BinaryUtil.GetBit(operand, index);
        _registers.Subtraction = false;
        _registers.HalfCarry = true;

        return inst.Operand == Register8.HLAsPointer ? 3ul : 2;
    }

    private ulong PrefixedSetBit(PrefixedInstruction.SetBit inst)
    {
        var operand = GetRegister8(inst.Operand);
        var index = inst.Index.ToInt();

        operand = BinaryUtil.SetBit(operand, index, true);
        SetRegister8(inst.Operand, operand);

        return inst.Operand == Register8.HLAsPointer ? 4ul : 2;
    }

    private ulong PrefixedResetBit(PrefixedInstruction.ResetBit inst)
    {
        var operand = GetRegister8(inst.Operand);
        var index = inst.Index.ToInt();

        operand = BinaryUtil.SetBit(operand, index, false);
        SetRegister8(inst.Operand, operand);

        return inst.Operand == Register8.HLAsPointer ? 4ul : 2;
    }
}
