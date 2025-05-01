using Boii.Abstractions;
using Boii.Errors;

namespace Boii.IO;

public class RandomAccessMemory : IGenericIO
{
    private readonly string _location;
    private readonly byte[] _buffer;

    private RandomAccessMemory(string location, int size) => (_location, _buffer) = (location, new byte[size]);

    public static RandomAccessMemory Create(string location, int size) => new(location, size);

    public byte Read(ushort address)
    {
        if (address <= _buffer.Length)
            return _buffer[address];

        throw SegmentationFault.Create(_location, address);
    }

    public void Write(ushort address, byte value)
    {
        if (address <= _buffer.Length)
            _buffer[address] = value;
        else
            throw SegmentationFault.Create(_location, address);
    }
}
