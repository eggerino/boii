namespace Boii.Util;

public static class BinaryUtil
{
    public static (byte high, byte low) ToBytes(ushort value) => ((byte)(value >> 8), (byte)value);

    public static ushort ToUShort(byte high, byte low) => (ushort)(high << 8 | low);

    public static bool GetBit(ushort value, int index) => ((value >> index) & 1) == 1;
    
    public static bool GetBit(byte value, int index) => GetBit((ushort)value, index);

    public static ushort SetBit(ushort source, int index, bool value) => value switch
    {
        true => (ushort)(source | (1 << index)),
        false => (ushort)(source & ~(1 << index)),
    };

    public static byte SetBit(byte source, int index, bool value) => (byte)SetBit((ushort)source, index, value);

    public static byte Slice(byte source, int index, int length)
    {
        var mask = ~(~0 << length);
        return (byte)((source >> index) & mask);
    }
}
