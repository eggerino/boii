using Boii.Errors;

namespace Boii.Memory;

public static class BufferAccesser
{
    public static byte Read(byte[] buffer, ushort address, string location, int offset = 0)
    {
        address = (ushort)(address - offset);
        if (address >= buffer.Length)
            throw SegmentationFault.Create(location, address);
        
        return buffer[address];
    }

    public static void Write(byte[] buffer, ushort address, byte value, string location, int offset = 0)
    {
        address = (ushort)(address - offset);
        if (address >= buffer.Length)
            throw SegmentationFault.Create(location, address);
        
        buffer[address] = value;
    }
}
