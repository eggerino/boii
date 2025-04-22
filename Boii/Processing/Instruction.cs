namespace Boii.Processing;

public abstract record Instruction
{
    public sealed record Nop : Instruction;

    public static Instruction? FromOpcode(byte opcode) => opcode switch
    {
        0x00 => new Nop(),
        _ => null,
    };
}
