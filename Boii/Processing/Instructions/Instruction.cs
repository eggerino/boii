using System.Linq;
using static Boii.Processing.Instructions.EncodedArgument;

namespace Boii.Processing.Instructions;

internal abstract record Instruction
{
    // Misc
    public sealed record Nop : Instruction;
    public sealed record Stop : Instruction;
    public sealed record DecimalAdjustA : Instruction;

    // Interrupt
    public sealed record Halt : Instruction;
    public sealed record EnableInterrupt : Instruction;
    public sealed record DisableInterrupt : Instruction;

    // Load
    public sealed record LoadLiteral8(Register8 Destination) : Instruction;
    public sealed record LoadRegister8(Register8 Source, Register8 Destination) : Instruction;
    public sealed record LoadLiteral16(Register16 Destination) : Instruction;
    public sealed record LoadFromA(Register16Memory Destination) : Instruction;
    public sealed record LoadFromAIntoLiteral16Pointer : Instruction;
    public sealed record LoadFromAIntoLiteral8HighPointer : Instruction;
    public sealed record LoadFromAIntoCHighPointer : Instruction;
    public sealed record LoadIntoA(Register16Memory Source) : Instruction;
    public sealed record LoadFromLiteral16PointerIntoA : Instruction;
    public sealed record LoadFromLiteral8HighPointerIntoA : Instruction;
    public sealed record LoadFromCHighPointerIntoA : Instruction;

    // 8 Bit arithmetic
    public sealed record IncrementRegister8(Register8 Operand) : Instruction;
    public sealed record DecrementRegister8(Register8 Operand) : Instruction;
    public sealed record AddToA(Register8 Operand) : Instruction;
    public sealed record AddLiteral8ToA : Instruction;
    public sealed record AddToACarry(Register8 Operand) : Instruction;
    public sealed record AddLiteral8ToACarry : Instruction;
    public sealed record SubtractFromA(Register8 Operand) : Instruction;
    public sealed record SubtractLiteral8FromA : Instruction;
    public sealed record SubtractFromACarry(Register8 Operand) : Instruction;
    public sealed record SubtractLiteral8FromACarry : Instruction;
    public sealed record CompareToA(Register8 Operand) : Instruction;
    public sealed record CompareLiteral8ToA : Instruction;

    // 16 Bit arithmetic
    public sealed record IncrementRegister16(Register16 Operand) : Instruction;
    public sealed record DecrementRegister16(Register16 Operand) : Instruction;
    public sealed record AddRegister16ToHL(Register16 Operand) : Instruction;

    // Bitwise logic
    public sealed record ComplementA : Instruction;
    public sealed record AndWithA(Register8 Operand) : Instruction;
    public sealed record AndLiteral8WithA : Instruction;
    public sealed record XorWithA(Register8 Operand) : Instruction;
    public sealed record XorLiteral8WithA : Instruction;
    public sealed record OrWithA(Register8 Operand) : Instruction;
    public sealed record OrLiteral8WithA : Instruction;

    // Bit shift
    public sealed record RotateLeftA : Instruction;
    public sealed record RotateLeftCarryA : Instruction;
    public sealed record RotateRightA : Instruction;
    public sealed record RotateRightCarryA : Instruction;

    // Jump and subroutine
    public sealed record JumpRelative : Instruction;
    public sealed record ConditionalJumpRelative(Condition Condition) : Instruction;
    public sealed record Jump : Instruction;
    public sealed record ConditionalJump(Condition Condition) : Instruction;
    public sealed record JumpHL : Instruction;
    public sealed record Call : Instruction;
    public sealed record ConditionalCall(Condition Condition) : Instruction;
    public sealed record Restart(U3 Target) : Instruction;
    public sealed record Return : Instruction;
    public sealed record ConditionalReturn(Condition Condition) : Instruction;
    public sealed record ReturnInterrupt : Instruction;

    // Carry flag
    public sealed record SetCarryFlag : Instruction;
    public sealed record ComplementCarryFlag : Instruction;

    // Stack manipulation
    public sealed record Push(Register16Stack Register) : Instruction;
    public sealed record Pop(Register16Stack Register) : Instruction;
    public sealed record AddSignedLiteral8ToStackPointer : Instruction;
    public sealed record LoadFromStackPointerIntoLiteral16Pointer : Instruction;
    public sealed record LoadFromStackPointerPlusSignedLiteral8IntoHL : Instruction;
    public sealed record LoadFromHLIntoStackPointer : Instruction;

    // 16 Bit instructions
    public sealed record Prefixed : Instruction;

    private static readonly Instruction?[] _lookup = InitializeLookup();

    public static Instruction? FromOpcode(byte opcode) => _lookup[opcode];

    private static Instruction?[] InitializeLookup() => [.. Enumerable.Range(0, 0x100).Select(x => CreateFromOpcode((byte)x))];

    private static Instruction? CreateFromOpcode(byte opcode) => opcode switch
    {
        // Misc
        0x00 => new Nop(),
        0b0001_0000 => new Stop(),
        0b0010_0111 => new DecimalAdjustA(),

        // Interrupt
        0b0111_0110 => new Halt(),
        0b1111_1011 => new EnableInterrupt(),
        0b1111_0011 => new DisableInterrupt(),

        // Load
        var x when (x & 0b1100_0111) == 0b0000_0110 => new LoadLiteral8(ToRegister8(x, 3)),
        var x when (x & 0b1100_0000) == 0b0100_0000 => new LoadRegister8(
            Source: ToRegister8(x, 0),
            Destination: ToRegister8(x, 3)),    // Must be after halt, halt has same bit pattern as ld [hl], [hl]
        var x when (x & 0b1100_1111) == 0b0000_0001 => new LoadLiteral16(ToRegister16(x, 4)),
        var x when (x & 0b1100_1111) == 0b0000_0010 => new LoadFromA(ToRegister16Memory(x, 4)),
        0b1110_1010 => new LoadFromAIntoLiteral16Pointer(),
        0b1110_0000 => new LoadFromAIntoLiteral8HighPointer(),
        0b1110_0010 => new LoadFromAIntoCHighPointer(),
        var x when (x & 0b1100_1111) == 0b0000_1010 => new LoadIntoA(ToRegister16Memory(x, 4)),
        0b1111_1010 => new LoadFromLiteral16PointerIntoA(),
        0b1111_0000 => new LoadFromLiteral8HighPointerIntoA(),
        0b1111_0010 => new LoadFromCHighPointerIntoA(),

        // 8 Bit arithmetic
        var x when (x & 0b1100_0111) == 0b0000_0100 => new IncrementRegister8(ToRegister8(x, 3)),
        var x when (x & 0b1100_0111) == 0b0000_0101 => new DecrementRegister8(ToRegister8(x, 3)),
        var x when (x & 0b1111_1000) == 0b1000_0000 => new AddToA(ToRegister8(x, 0)),
        0b1100_0110 => new AddLiteral8ToA(),
        var x when (x & 0b1111_1000) == 0b1000_1000 => new AddToACarry(ToRegister8(x, 0)),
        0b1100_1110 => new AddLiteral8ToACarry(),
        var x when (x & 0b1111_1000) == 0b1001_0000 => new SubtractFromA(ToRegister8(x, 0)),
        0b1101_0110 => new SubtractLiteral8FromA(),
        var x when (x & 0b1111_1000) == 0b1001_1000 => new SubtractFromACarry(ToRegister8(x, 0)),
        0b1101_1110 => new SubtractLiteral8FromACarry(),
        var x when (x & 0b1111_1000) == 0b1011_1000 => new CompareToA(ToRegister8(x, 0)),
        0b1111_1110 => new CompareLiteral8ToA(),

        // 16 Bit arithmetic
        var x when (x & 0b1100_1111) == 0b0000_0011 => new IncrementRegister16(ToRegister16(x, 4)),
        var x when (x & 0b1100_1111) == 0b0000_1011 => new DecrementRegister16(ToRegister16(x, 4)),
        var x when (x & 0b1100_1111) == 0b0000_1001 => new AddRegister16ToHL(ToRegister16(x, 4)),

        // Bitwise logic
        0b0010_1111 => new ComplementA(),
        var x when (x & 0b1111_1000) == 0b1010_0000 => new AndWithA(ToRegister8(x, 0)),
        0b1110_0110 => new AndLiteral8WithA(),
        var x when (x & 0b1111_1000) == 0b1010_1000 => new XorWithA(ToRegister8(x, 0)),
        0b1110_1110 => new XorLiteral8WithA(),
        var x when (x & 0b1111_1000) == 0b1011_0000 => new OrWithA(ToRegister8(x, 0)),
        0b1111_0110 => new OrLiteral8WithA(),

        // Bit shift
        0b0000_0111 => new RotateLeftA(),
        0b0001_0111 => new RotateLeftCarryA(),
        0b0000_1111 => new RotateRightA(),
        0b0001_1111 => new RotateRightCarryA(),

        // Jump and subroutine
        0b0001_1000 => new JumpRelative(),
        var x when (x & 0b1110_0111) == 0b0010_0000 => new ConditionalJumpRelative(ToCondition(x, 3)),
        0b1100_0011 => new Jump(),
        var x when (x & 0b1110_0111) == 0b1100_0010 => new ConditionalJump(ToCondition(x, 3)),
        0b1110_1001 => new JumpHL(),
        0b1100_1101 => new Call(),
        var x when (x & 0b1110_0111) == 0b1100_0100 => new ConditionalCall(ToCondition(x, 3)),
        var x when (x & 0b1100_0111) == 0b1100_0111 => new Restart(ToU3(x, 3)),
        0b1100_1001 => new Return(),
        var x when (x & 0b1110_0111) == 0b1100_0000 => new ConditionalReturn(ToCondition(x, 3)),
        0b1101_1001 => new ReturnInterrupt(),

        // Carry flag
        0b0011_0111 => new SetCarryFlag(),
        0b0011_1111 => new ComplementCarryFlag(),

        // Stack manipulation
        var x when (x & 0b1100_1111) == 0b1100_0101 => new Push(ToRegister16Stack(x, 4)),
        var x when (x & 0b1100_1111) == 0b1100_0001 => new Pop(ToRegister16Stack(x, 4)),
        0b1110_1000 => new AddSignedLiteral8ToStackPointer(),
        0b0000_1000 => new LoadFromStackPointerIntoLiteral16Pointer(),
        0b1111_1000 => new LoadFromStackPointerPlusSignedLiteral8IntoHL(),
        0b1111_1001 => new LoadFromHLIntoStackPointer(),

        // 16 Bit instructions
        0xCB => new Prefixed(),

        _ => null,
    };
}
