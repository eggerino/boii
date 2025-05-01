using Boii.Abstractions;
using Boii.Errors;

namespace Boii.Memory;

public class ArrayMemory : IGenericIO
{
    private readonly string _location;
    private readonly byte[] _buffer;

    private ArrayMemory(string location, byte[] buffer) => (_location, _buffer) = (location, buffer);

    public static ArrayMemory Create(string location, int size) => new(location, new byte[size]);

    public static ArrayMemory From(string location, byte[] buffer) => new(location, buffer);

    public byte Read(ushort address)
    {
        if (address >= _buffer.Length)
            throw SegmentationFault.Create(_location, address);
        
        return _buffer[address];
    }

    public void Write(ushort address, byte value)
    {
        if (address >= _buffer.Length)
            throw SegmentationFault.Create(_location, address);

        _buffer[address] = value;
    }
}
