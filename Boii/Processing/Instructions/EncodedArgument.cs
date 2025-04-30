using Boii.Util;

namespace Boii.Processing.Instructions;

internal enum Register8 { B = 0, C, D, E, H, L, HLAsPointer, A, }
internal enum Register16 { BC = 0, DE, HL, StackPointer, }
internal enum Register16Stack { BC = 0, DE, HL, AF, }
internal enum Register16Memory { BC = 0, DE, HLInc, HLDec }
internal enum Condition { NotZero = 0, Zero, NotCarry, Carry }
internal enum U3 { Zero = 0, One, Two, Three, Four, Five, Six, Seven, }

internal static class EncodedArgument
{
    public static Register8 ToRegister8(byte opcode, int offset) => (Register8)BinaryUtil.Slice(opcode, offset, 3);
    public static Register16 ToRegister16(byte opcode, int offset) => (Register16)BinaryUtil.Slice(opcode, offset, 2);
    public static Register16Stack ToRegister16Stack(byte opcode, int offset) => (Register16Stack)BinaryUtil.Slice(opcode, offset, 2);
    public static Register16Memory ToRegister16Memory(byte opcode, int offset) => (Register16Memory)BinaryUtil.Slice(opcode, offset, 2);
    public static Condition ToCondition(byte opcode, int offset) => (Condition)BinaryUtil.Slice(opcode, offset, 2);
    public static U3 ToU3(byte opcode, int offset) => (U3)BinaryUtil.Slice(opcode, offset, 3);

    public static int ToInt(this U3 value) => (int)value;
}
