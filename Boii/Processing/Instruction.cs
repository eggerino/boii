using Boii.Util;

namespace Boii.Processing;

public abstract record Instruction
{
    public enum Register8 { B = 0, C, D, E, H, L, HLAsPointer, A }
    public enum Register16 { BC = 0, DE, HL, StackPointer }
    public enum Register16Stack { BC = 0, DE, HL, AF }
    public enum Register16Memory { BC = 0, DE, HLInc, HLDec }
    public enum Condition { NotZero = 0, Zero, NotCarry, Carry }

    public sealed record Nop : Instruction;
    public sealed record LoadImm8(Register8 Destination) : Instruction;
    public sealed record LoadImm16(Register16 Destination) : Instruction;
    public sealed record LoadFromA(Register16Memory Destination) : Instruction;

    public static Instruction? FromOpcode(byte opcode) => opcode switch
    {
        0x00 => new Nop(),
        var x when (x & 0b1100_0111) == 0b0000_0110 => new LoadImm8(ToRegister8(x, 3)),
        var x when (x & 0b1100_1111) == 0b0000_0001 => new LoadImm16(ToRegister16(x, 4)),
        var x when (x & 0b1100_1111) == 0b0000_0010 => new LoadFromA(ToRegister16Memory(x, 4)),
        _ => null,
    };

    private static Register8 ToRegister8(byte opcode, int offset) => (Register8)BinaryUtil.Slice(opcode, offset, 3);
    private static Register16 ToRegister16(byte opcode, int offset) => (Register16)BinaryUtil.Slice(opcode, offset, 2);
    private static Register16Stack ToRegister16Stack(byte opcode, int offset) => (Register16Stack)BinaryUtil.Slice(opcode, offset, 2);
    private static Register16Memory ToRegister16Memory(byte opcode, int offset) => (Register16Memory)BinaryUtil.Slice(opcode, offset, 2);
    private static Condition ToCondition(byte opcode, int offset) => (Condition)BinaryUtil.Slice(opcode, offset, 2);
}
