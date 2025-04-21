namespace Boii.Util;

public static class BinaryUtil
{
    public static (byte high, byte low) ToBytes(ushort value) => ((byte)(value >> 8), (byte)value);

    public static ushort ToUShort(byte high, byte low) => (ushort)(high << 8 | low);
}
