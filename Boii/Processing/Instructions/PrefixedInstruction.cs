using System.Diagnostics;
using System.Linq;
using Boii.Errors;
using static Boii.Processing.Instructions.EncodedArgument;

namespace Boii.Processing.Instructions;

internal abstract record PrefixedInstruction
{
    // Bit shift
    public sealed record RotateLeft(Register8 Operand) : PrefixedInstruction;
    public sealed record RotateLeftThroughCarry(Register8 Operand) : PrefixedInstruction;
    public sealed record RotateRight(Register8 Operand) : PrefixedInstruction;
    public sealed record RotateRightThroughCarry(Register8 Operand) : PrefixedInstruction;
    public sealed record ShiftLeftArithmetic(Register8 Operand) : PrefixedInstruction;
    public sealed record ShiftRightArithmetic(Register8 Operand) : PrefixedInstruction;
    public sealed record Swap(Register8 Operand) : PrefixedInstruction;
    public sealed record ShiftRightLogical(Register8 Operand) : PrefixedInstruction;

    // Bit flag
    public sealed record CheckBit(Register8 Operand, U3 Index) : PrefixedInstruction;
    public sealed record SetBit(Register8 Operand, U3 Index) : PrefixedInstruction;
    public sealed record ResetBit(Register8 Operand, U3 Index) : PrefixedInstruction;

    private static readonly PrefixedInstruction[] _lookup = InitializeLookup();

    public static PrefixedInstruction FromOpcode(byte opcode) => _lookup[opcode];

    private static PrefixedInstruction[] InitializeLookup() => [.. Enumerable.Range(0, 0x100).Select(x => CreateFromOpcode((byte)x))];

    private static PrefixedInstruction CreateFromOpcode(byte opcode) => opcode switch
    {
        var x when (x & 0b1111_1000) == 0b0000_0000 => new RotateLeft(ToRegister8(x, 0)),
        var x when (x & 0b1111_1000) == 0b0000_1000 => new RotateRight(ToRegister8(x, 0)),
        var x when (x & 0b1111_1000) == 0b0001_0000 => new RotateLeftThroughCarry(ToRegister8(x, 0)),
        var x when (x & 0b1111_1000) == 0b0001_1000 => new RotateRightThroughCarry(ToRegister8(x, 0)),
        var x when (x & 0b1111_1000) == 0b0010_0000 => new ShiftLeftArithmetic(ToRegister8(x, 0)),
        var x when (x & 0b1111_1000) == 0b0010_1000 => new ShiftRightArithmetic(ToRegister8(x, 0)),
        var x when (x & 0b1111_1000) == 0b0011_0000 => new Swap(ToRegister8(x, 0)),
        var x when (x & 0b1111_1000) == 0b0011_1000 => new ShiftRightLogical(ToRegister8(x, 0)),
        var x when (x & 0b1100_0000) == 0b0100_0000 => new CheckBit(ToRegister8(x, 0), ToU3(x, 3)),
        var x when (x & 0b1100_0000) == 0b1000_0000 => new ResetBit(ToRegister8(x, 0), ToU3(x, 3)),
        var x when (x & 0b1100_0000) == 0b1100_0000 => new SetBit(ToRegister8(x, 0), ToU3(x, 3)),
        _ => throw PatternMatchingError.Create(opcode),
    };
}
