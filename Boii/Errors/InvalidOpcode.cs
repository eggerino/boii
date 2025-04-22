using System;

namespace Boii.Errors;

public class InvalidOpcode : Exception
{
    public byte Opcode { get; }
    public ushort Address { get; }

    private InvalidOpcode(byte opcode, ushort address) : base($"Invalid opcode of {opcode} at 0x{address:X}") =>
        (Opcode, Address) = (opcode, address);

    public static InvalidOpcode Create(byte opcode, ushort address) => new(opcode, address);
}
