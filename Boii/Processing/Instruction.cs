using Boii.Util;

namespace Boii.Processing;

public abstract record Instruction
{
    public enum Register8 { B = 0, C, D, E, H, L, HLAsPointer, A }
    public enum Register16 { BC = 0, DE, HL, StackPointer }
    public enum Register16Stack { BC = 0, DE, HL, AF }
    public enum Register16Memory { BC = 0, DE, HLInc, HLDec }
    public enum JumpCondition { NotZero = 0, Zero, NotCarry, Carry }

    public sealed record Nop : Instruction;

    public sealed record LoadImm8(Register8 Destination) : Instruction;
    public sealed record LoadImm16(Register16 Destination) : Instruction;
    public sealed record LoadFromA(Register16Memory Destination) : Instruction;
    public sealed record LoadIntoA(Register16Memory Source) : Instruction;
    public sealed record LoadFromStackPointer : Instruction;

    public sealed record IncrementRegister8(Register8 Operand) : Instruction;
    public sealed record DecrementRegister8(Register8 Operand) : Instruction;

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

    public sealed record JumpRelative : Instruction;
    public sealed record ConditionalJumpRelative(JumpCondition Condition) : Instruction;

    public static Instruction? FromOpcode(byte opcode) => opcode switch
    {
        0x00 => new Nop(),

        var x when (x & 0b1100_0111) == 0b0000_0110 => new LoadImm8(ToRegister8(x, 3)),
        var x when (x & 0b1100_1111) == 0b0000_0001 => new LoadImm16(ToRegister16(x, 4)),
        var x when (x & 0b1100_1111) == 0b0000_0010 => new LoadFromA(ToRegister16Memory(x, 4)),
        var x when (x & 0b1100_1111) == 0b0000_1010 => new LoadIntoA(ToRegister16Memory(x, 4)),
        0b0000_1000 => new LoadFromStackPointer(),

        var x when (x & 0b1100_0111) == 0b0000_0100 => new IncrementRegister8(ToRegister8(x, 3)),
        var x when (x & 0b1100_0111) == 0b0000_0101 => new DecrementRegister8(ToRegister8(x, 3)),

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

        0b0001_1000 => new JumpRelative(),
        var x when (x & 0b1110_0111) == 0b0010_0000 => new ConditionalJumpRelative(ToCondition(x, 3)),

        _ => null,
    };

    private static Register8 ToRegister8(byte opcode, int offset) => (Register8)BinaryUtil.Slice(opcode, offset, 3);
    private static Register16 ToRegister16(byte opcode, int offset) => (Register16)BinaryUtil.Slice(opcode, offset, 2);
    private static Register16Stack ToRegister16Stack(byte opcode, int offset) => (Register16Stack)BinaryUtil.Slice(opcode, offset, 2);
    private static Register16Memory ToRegister16Memory(byte opcode, int offset) => (Register16Memory)BinaryUtil.Slice(opcode, offset, 2);
    private static JumpCondition ToCondition(byte opcode, int offset) => (JumpCondition)BinaryUtil.Slice(opcode, offset, 2);
}
