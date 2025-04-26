using Boii.Util;

namespace Boii.Processing;

internal abstract record Instruction
{
    public enum Register8 { B = 0, C, D, E, H, L, HLAsPointer, A }
    public enum Register16 { BC = 0, DE, HL, StackPointer }
    public enum Register16Stack { BC = 0, DE, HL, AF }
    public enum Register16Memory { BC = 0, DE, HLInc, HLDec }
    public enum JumpCondition { NotZero = 0, Zero, NotCarry, Carry }

    public sealed record Nop : Instruction;
    public sealed record Stop : Instruction;
    public sealed record Halt : Instruction;

    public sealed record LoadImm8(Register8 Destination) : Instruction;
    public sealed record LoadRegister8ToRegister8(Register8 Source, Register8 Destination) : Instruction;
    public sealed record LoadImm16(Register16 Destination) : Instruction;
    public sealed record LoadFromA(Register16Memory Destination) : Instruction;
    public sealed record LoadIntoA(Register16Memory Source) : Instruction;
    public sealed record LoadFromStackPointer : Instruction;

    public sealed record IncrementRegister8(Register8 Operand) : Instruction;
    public sealed record DecrementRegister8(Register8 Operand) : Instruction;

    public sealed record AddToA(Register8 Operand) : Instruction;
    public sealed record AddToAImm8 : Instruction;
    public sealed record AddToACarry(Register8 Operand) : Instruction;
    public sealed record AddToAImm8Carry : Instruction;
    public sealed record SubtractToA(Register8 Operand) : Instruction;
    public sealed record SubtractToAImm8 : Instruction;
    public sealed record SubtractToACarry(Register8 Operand) : Instruction;
    public sealed record SubtractToAImm8Carry : Instruction;
    public sealed record CompareToA(Register8 Operand) : Instruction;
    public sealed record CompareToAImm8 : Instruction;

    public sealed record IncrementRegister16(Register16 Operand) : Instruction;
    public sealed record DecrementRegister16(Register16 Operand) : Instruction;
    public sealed record AddRegister16ToHL(Register16 Operand) : Instruction;

    public sealed record RotateLeftA : Instruction;
    public sealed record RotateRightA : Instruction;
    public sealed record RotateLeftCarryA : Instruction;
    public sealed record RotateRightCarryA : Instruction;

    public sealed record DecimalAdjustAccumulator : Instruction;
    public sealed record ComplementAccumulator : Instruction;

    public sealed record SetCarryFlag : Instruction;
    public sealed record ComplementCarryFlag : Instruction;

    public sealed record AndToA(Register8 Operand) : Instruction;
    public sealed record AndToAImm8 : Instruction;
    public sealed record XorToA(Register8 Operand) : Instruction;
    public sealed record XorToAImm8 : Instruction;
    public sealed record OrToA(Register8 Operand) : Instruction;
    public sealed record OrToAImm8 : Instruction;

    public sealed record JumpRelative : Instruction;
    public sealed record ConditionalJumpRelative(JumpCondition Condition) : Instruction;
    public sealed record Jump : Instruction;
    public sealed record ConditionalJump(JumpCondition Condition) : Instruction;
    public sealed record JumpHL : Instruction;

    public sealed record Call : Instruction;
    public sealed record ConditionalCall(JumpCondition Condition) : Instruction;
    public sealed record Restart(byte Target) : Instruction;
    public sealed record Return : Instruction;
    public sealed record ConditionalReturn(JumpCondition Condition) : Instruction;
    public sealed record ReturnInterrupt : Instruction;

    public static Instruction? FromOpcode(byte opcode) => opcode switch
    {
        0x00 => new Nop(),
        0b0001_0000 => new Stop(),
        0b0111_0110 => new Halt(),

        var x when (x & 0b1100_0111) == 0b0000_0110 => new LoadImm8(ToRegister8(x, 3)),
        var x when (x & 0b1100_0000) == 0b0100_0000 => new LoadRegister8ToRegister8(        // LoadRegister8toRegister8 must be after halt
            Source: ToRegister8(x, 0),
            Destination: ToRegister8(x, 3)),
        var x when (x & 0b1100_1111) == 0b0000_0001 => new LoadImm16(ToRegister16(x, 4)),
        var x when (x & 0b1100_1111) == 0b0000_0010 => new LoadFromA(ToRegister16Memory(x, 4)),
        var x when (x & 0b1100_1111) == 0b0000_1010 => new LoadIntoA(ToRegister16Memory(x, 4)),
        0b0000_1000 => new LoadFromStackPointer(),

        var x when (x & 0b1100_0111) == 0b0000_0100 => new IncrementRegister8(ToRegister8(x, 3)),
        var x when (x & 0b1100_0111) == 0b0000_0101 => new DecrementRegister8(ToRegister8(x, 3)),

        var x when (x & 0b1111_1000) == 0b1000_0000 => new AddToA(ToRegister8(x, 0)),
        0b1100_0110 => new AddToAImm8(),
        var x when (x & 0b1111_1000) == 0b1000_1000 => new AddToACarry(ToRegister8(x, 0)),
        0b1100_1110 => new AddToAImm8Carry(),
        var x when (x & 0b1111_1000) == 0b1001_0000 => new SubtractToA(ToRegister8(x, 0)),
        0b1101_0110 => new SubtractToAImm8(),
        var x when (x & 0b1111_1000) == 0b1001_1000 => new SubtractToACarry(ToRegister8(x, 0)),
        0b1101_1110 => new SubtractToAImm8Carry(),
        var x when (x & 0b1111_1000) == 0b1011_1000 => new CompareToA(ToRegister8(x, 0)),
        0b1111_1110 => new CompareToAImm8(),

        var x when (x & 0b1100_1111) == 0b0000_0011 => new IncrementRegister16(ToRegister16(x, 4)),
        var x when (x & 0b1100_1111) == 0b0000_1011 => new DecrementRegister16(ToRegister16(x, 4)),
        var x when (x & 0b1100_1111) == 0b0000_1001 => new AddRegister16ToHL(ToRegister16(x, 4)),

        0b0000_0111 => new RotateLeftA(),
        0b0000_1111 => new RotateRightA(),
        0b0001_0111 => new RotateLeftCarryA(),
        0b0001_1111 => new RotateRightCarryA(),

        0b0010_0111 => new DecimalAdjustAccumulator(),
        0b0010_1111 => new ComplementAccumulator(),

        0b0011_0111 => new SetCarryFlag(),
        0b0011_1111 => new ComplementCarryFlag(),

        var x when (x & 0b1111_1000) == 0b1010_0000 => new AndToA(ToRegister8(x, 0)),
        0b1110_0110 => new AndToAImm8(),
        var x when (x & 0b1111_1000) == 0b1010_1000 => new XorToA(ToRegister8(x, 0)),
        0b1110_1110 => new XorToAImm8(),
        var x when (x & 0b1111_1000) == 0b1011_0000 => new OrToA(ToRegister8(x, 0)),
        0b1111_0110 => new OrToAImm8(),

        0b0001_1000 => new JumpRelative(),
        var x when (x & 0b1110_0111) == 0b0010_0000 => new ConditionalJumpRelative(ToCondition(x, 3)),
        0b1100_0011 => new Jump(),
        var x when (x & 0b1110_0111) == 0b1100_0010 => new ConditionalJump(ToCondition(x, 3)),
        0b1110_1001 => new JumpHL(),

        0b1100_1101 => new Call(),
        var x when (x & 0b1110_0111) == 0b1100_0100 => new ConditionalCall(ToCondition(x, 3)),
        var x when (x & 0b1100_0111) == 0b1100_0111 => new Restart(BinaryUtil.Slice(x, 3, 3)),
        0b1100_1001 => new Return(),
        var x when (x & 0b1110_0111) == 0b1100_0000 => new ConditionalReturn(ToCondition(x, 3)),
        0b1101_1001 => new ReturnInterrupt(),

        _ => null,
    };

    private static Register8 ToRegister8(byte opcode, int offset) => (Register8)BinaryUtil.Slice(opcode, offset, 3);
    private static Register16 ToRegister16(byte opcode, int offset) => (Register16)BinaryUtil.Slice(opcode, offset, 2);
    private static Register16Stack ToRegister16Stack(byte opcode, int offset) => (Register16Stack)BinaryUtil.Slice(opcode, offset, 2);
    private static Register16Memory ToRegister16Memory(byte opcode, int offset) => (Register16Memory)BinaryUtil.Slice(opcode, offset, 2);
    private static JumpCondition ToCondition(byte opcode, int offset) => (JumpCondition)BinaryUtil.Slice(opcode, offset, 2);
}
