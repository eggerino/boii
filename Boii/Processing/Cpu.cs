using System;
using System.Linq;
using Boii.Abstractions;
using Boii.Errors;
using Boii.Processing.Instructions;
using Boii.Util;

namespace Boii.Processing;

public class Cpu
{
    // Interrupt
    private ulong Halt(Instruction.Halt _)
    {
        _halted = true;

        // Halt bug
        if (!_interruptMaster.Value && GetPendingInterrupts() != 0)
        {
            // Halt immediately exits on the bugged case
            _halted = false;

            var previousOpcode = _bus.Read((ushort)(_registers.ProgramCounter - 2));
            var nextOpcode = _bus.Read(_registers.ProgramCounter);

            if (Instruction.FromOpcode(previousOpcode) is Instruction.EnableInterrupt)
            {
                // Interrupt will get fired and must return to the halt itself
                _registers.ProgramCounter--;
            }
            else
            {
                // Regular duplication bug -> cpu immediately wakes up and next byte gets executed twice
                _bufferedOpcode = nextOpcode;
            }
        }

        return 1;
    }

    private ulong EnableInterrupt(Instruction.EnableInterrupt _)
    {
        _interruptMaster.Enque(true, 1);    // Delay for one step
        return 1;
    }

    private ulong DisableInterrupt(Instruction.DisableInterrupt _)
    {
        _interruptMaster.Enque(false, 0);
        return 1;
    }

    // Load
    private ulong LoadLiteral8(Instruction.LoadLiteral8 inst)
    {
        var value = FetchByte();
        SetRegister8(inst.Destination, value);
        return inst.Destination == Register8.HLAsPointer ? 3ul : 2ul;
    }

    private ulong LoadRegister8(Instruction.LoadRegister8 inst)
    {
        var value = GetRegister8(inst.Source);
        SetRegister8(inst.Destination, value);
        return inst.Source == Register8.HLAsPointer || inst.Destination == Register8.HLAsPointer ? 2ul : 1;
    }

    private ulong LoadLiteral16(Instruction.LoadLiteral16 inst)
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

    private ulong LoadFromAIntoLiteral16Pointer(Instruction.LoadFromAIntoLiteral16Pointer _)
    {
        var address = FetchUShort();
        _bus.Write(address, _registers.A);
        return 4;
    }

    private ulong LoadFromAIntoLiteral8HighPointer(Instruction.LoadFromAIntoLiteral8HighPointer _)
    {
        var address = (ushort)(0xFF00 + FetchByte());
        _bus.Write(address, _registers.A);
        return 3;
    }

    private ulong LoadFromAIntoCHighPointer(Instruction.LoadFromAIntoCHighPointer _)
    {
        var address = (ushort)(0xff00 + _registers.C);
        _bus.Write(address, _registers.A);
        return 2;
    }

    private ulong LoadIntoA(Instruction.LoadIntoA inst)
    {
        var value = ReadFromRegister16Memory(inst.Source);
        _registers.A = value;
        return 2;
    }

    private ulong LoadFromLiteral16PointerIntoA(Instruction.LoadFromLiteral16PointerIntoA _)
    {
        var address = FetchUShort();
        _registers.A = _bus.Read(address);
        return 4;
    }

    private ulong LoadFromLiteral8HighPointerIntoA(Instruction.LoadFromLiteral8HighPointerIntoA _)
    {
        var address = (ushort)(0xFF00 + FetchByte());
        _registers.A = _bus.Read(address);
        return 3;
    }

    private ulong LoadFromCHighPointerIntoA(Instruction.LoadFromCHighPointerIntoA _)
    {
        var address = (ushort)(0xff00 + _registers.C);
        _registers.A = _bus.Read(address);
        return 2;
    }

    // 8 Bit arithmetic
    private ulong IncrementRegister8(Instruction.IncrementRegister8 inst)
    {
        int value = GetRegister8(inst.Operand);
        value++;
        SetRegister8(inst.Operand, (byte)value);

        _registers.Zero = ((byte)value) == 0;
        _registers.Subtraction = false;
        _registers.HalfCarry = IsOverflowBit3(value - 1, 1);

        return inst.Operand == Register8.HLAsPointer ? 3ul : 1ul;
    }

    private ulong DecrementRegister8(Instruction.DecrementRegister8 inst)
    {
        int value = GetRegister8(inst.Operand);
        value--;
        SetRegister8(inst.Operand, (byte)value);

        _registers.Zero = ((byte)value) == 0;
        _registers.Subtraction = true;
        _registers.HalfCarry = IsBorrowBit4(value + 1, 1);

        return inst.Operand == Register8.HLAsPointer ? 3ul : 1ul;
    }

    private ulong AddToA(Instruction.AddToA inst)
    {
        var operand = GetRegister8(inst.Operand);
        DoAddA(operand, false);
        return inst.Operand == Register8.HLAsPointer ? 2ul : 1;
    }

    private ulong AddLiteral8ToA(Instruction.AddLiteral8ToA _)
    {
        var operand = FetchByte();
        DoAddA(operand, false);
        return 2;
    }

    private ulong AddToACarry(Instruction.AddToACarry inst)
    {
        var operand = GetRegister8(inst.Operand);
        DoAddA(operand, _registers.Carry);
        return inst.Operand == Register8.HLAsPointer ? 2ul : 1;
    }

    private ulong AddLiteral8ToACarry(Instruction.AddLiteral8ToACarry _)
    {
        var operand = FetchByte();
        DoAddA(operand, _registers.Carry);
        return 2;
    }

    private void DoAddA(byte increment, bool carry)
    {
        int operand = increment;
        if (carry) operand++;

        var oldValue = _registers.A;
        var newValue = oldValue + operand;

        _registers.A = (byte)newValue;
        _registers.Zero = ((byte)newValue) == 0;
        _registers.Subtraction = false;
        _registers.HalfCarry = IsOverflowBit3(oldValue, operand);
        _registers.Carry = IsOverflowBit7(oldValue, operand);
    }

    private ulong SubtractFromA(Instruction.SubtractFromA inst)
    {
        var operand = GetRegister8(inst.Operand);
        DoSubtractA(operand, false);
        return inst.Operand == Register8.HLAsPointer ? 2ul : 1;
    }

    private ulong SubtractLiteral8FromA(Instruction.SubtractLiteral8FromA _)
    {
        var operand = FetchByte();
        DoSubtractA(operand, false);
        return 2;
    }

    private ulong SubtractFromACarry(Instruction.SubtractFromACarry inst)
    {
        var operand = GetRegister8(inst.Operand);
        DoSubtractA(operand, _registers.Carry);
        return inst.Operand == Register8.HLAsPointer ? 2ul : 1;
    }

    private ulong SubtractLiteral8FromACarry(Instruction.SubtractLiteral8FromACarry _)
    {
        var operand = FetchByte();
        DoSubtractA(operand, _registers.Carry);
        return 2;
    }

    private void DoSubtractA(byte decrement, bool carry)
    {
        int operand = decrement;
        if (carry) operand++;

        var oldValue = _registers.A;
        var newValue = oldValue - operand;

        _registers.A = (byte)newValue;
        _registers.Zero = ((byte)newValue) == 0;
        _registers.Subtraction = true;
        _registers.HalfCarry = IsBorrowBit4(oldValue, operand);
        _registers.Carry = operand > oldValue;
    }

    private ulong CompareToA(Instruction.CompareToA inst)
    {
        var operand = GetRegister8(inst.Operand);
        DoCompareA(operand);
        return inst.Operand == Register8.HLAsPointer ? 2ul : 1;
    }

    private ulong CompareLiteral8ToA(Instruction.CompareLiteral8ToA _)
    {
        var operand = FetchByte();
        DoCompareA(operand);
        return 2;
    }

    private void DoCompareA(byte operand)
    {
        var a = _registers.A;
        var checkValue = a - operand;

        _registers.Zero = ((byte)checkValue) == 0;
        _registers.Subtraction = true;
        _registers.HalfCarry = IsBorrowBit4(a, operand);
        _registers.Carry = operand > a;
    }

    // 16 Bit arithmetic
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
        _registers.HalfCarry = IsOverflowBit11(oldValue, operand);
        _registers.Carry = IsOverflowBit15(oldValue, operand);

        return 2;
    }

    // Bitwise logic
    private ulong ComplementA(Instruction.ComplementA _)
    {
        _registers.A = (byte)~_registers.A;
        _registers.Subtraction = true;
        _registers.HalfCarry = true;

        return 1;
    }

    private ulong AndWithA(Instruction.AndWithA inst)
    {
        var operand = GetRegister8(inst.Operand);
        DoAndA(operand);
        return inst.Operand == Register8.HLAsPointer ? 2ul : 1;
    }

    private ulong AndLiteral8WithA(Instruction.AndLiteral8WithA _)
    {
        var operand = FetchByte();
        DoAndA(operand);
        return 2;
    }

    private void DoAndA(byte operand)
    {
        _registers.A &= operand;
        _registers.Zero = _registers.A == 0;
        _registers.Subtraction = false;
        _registers.HalfCarry = true;
        _registers.Carry = false;
    }

    private ulong XorWithA(Instruction.XorWithA inst)
    {
        var operand = GetRegister8(inst.Operand);
        DoXorA(operand);
        return inst.Operand == Register8.HLAsPointer ? 2ul : 1;
    }

    private ulong XorLiteral8WithA(Instruction.XorLiteral8WithA _)
    {
        var operand = FetchByte();
        DoXorA(operand);
        return 2;
    }

    private void DoXorA(byte operand)
    {
        _registers.A ^= operand;
        _registers.Zero = _registers.A == 0;
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = false;
    }

    private ulong OrWithA(Instruction.OrWithA inst)
    {
        var operand = GetRegister8(inst.Operand);
        DoOrA(operand);
        return inst.Operand == Register8.HLAsPointer ? 2ul : 1;
    }

    private ulong OrLiteral8WithA(Instruction.OrLiteral8WithA _)
    {
        var operand = FetchByte();
        DoOrA(operand);
        return 2;
    }

    private void DoOrA(byte operand)
    {
        _registers.A |= operand;
        _registers.Zero = _registers.A == 0;
        _registers.Subtraction = false;
        _registers.HalfCarry = false;
        _registers.Carry = false;
    }

    // Bit shift
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

    // Jump and subroutine
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
            _registers.ProgramCounter = (ushort)(_registers.ProgramCounter + offset);

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

        if (condition)
            _registers.ProgramCounter = target;

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
        DoCall(address);
        return 6;
    }

    private ulong ConditionalCall(Instruction.ConditionalCall inst)
    {
        var address = FetchUShort();
        var condition = GetCondition(inst.Condition);

        if (!condition)
            return 3;

        DoCall(address);
        return 6;
    }

    private ulong Restart(Instruction.Restart inst)
    {
        var address = inst.Target.ToInt() * 8;
        DoCall((ushort)address);
        return 4;
    }


    private ulong Return(Instruction.Return _)
    {
        DoReturn();
        return 4;
    }

    private ulong ConditionalReturn(Instruction.ConditionalReturn inst)
    {
        var condition = GetCondition(inst.Condition);
        if (!condition)
            return 2;

        DoReturn();
        return 5;
    }

    private ulong ReturnInterrupt(Instruction.ReturnInterrupt _)
    {
        DoReturn();
        _interruptMaster.Enque(true, 0);    // Is immediately after the return
        return 4;
    }

    private void DoReturn()
    {
        var low = _bus.Read(_registers.StackPointer++);
        var high = _bus.Read(_registers.StackPointer++);
        var returnAddress = BinaryUtil.ToUShort(high, low);

        _registers.ProgramCounter = returnAddress;
    }

    // Carry flag
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
