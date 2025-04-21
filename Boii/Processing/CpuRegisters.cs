using Boii.Util;

namespace Boii.Processing;

public class CpuRegisters
{
    public byte A { get; set; } = 0;
    public byte F { get; set; } = 0;
    public byte B { get; set; } = 0;
    public byte C { get; set; } = 0;
    public byte D { get; set; } = 0;
    public byte E { get; set; } = 0;
    public byte H { get; set; } = 0;
    public byte L { get; set; } = 0;
    public ushort StackPointer { get; set; } = 0;
    public ushort ProgramCounter { get; set; } = 0x0100;

    private CpuRegisters() { }

    public static CpuRegisters Create() => new();

    public ushort AF { get => BinaryUtil.ToUShort(high: A, low: F); set => (A, F) = BinaryUtil.ToBytes(value); }
    public ushort BC { get => BinaryUtil.ToUShort(high: B, low: C); set => (B, C) = BinaryUtil.ToBytes(value); }
    public ushort DE { get => BinaryUtil.ToUShort(high: D, low: E); set => (D, E) = BinaryUtil.ToBytes(value); }
    public ushort HL { get => BinaryUtil.ToUShort(high: H, low: L); set => (H, L) = BinaryUtil.ToBytes(value); }

    public bool Zero { get => BinaryUtil.GetBit(F, 7); set => F = BinaryUtil.SetBit(F, 7, value); }
    public bool Subtraction { get => BinaryUtil.GetBit(F, 6); set => F = BinaryUtil.SetBit(F, 6, value); }
    public bool HalfCarry { get => BinaryUtil.GetBit(F, 5); set => F = BinaryUtil.SetBit(F, 5, value); }
    public bool Carry { get => BinaryUtil.GetBit(F, 4); set => F = BinaryUtil.SetBit(F, 4, value); }
}
