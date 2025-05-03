using System;
using Boii.Abstractions;
using Boii.Memory;

namespace Boii.Graphics;

public class ObjectAttributeMemory : IGenericIO
{
    private readonly byte[] _buffer = new byte[40 * 4];

    private ObjectAttributeMemory() { }

    public static ObjectAttributeMemory Create() => new();

    public byte Read(ushort address) => BufferAccesser.Read(_buffer, address, "ObjectAttributeMemory");

    public void Write(ushort address, byte value) => BufferAccesser.Write(_buffer, address, value, "ObjectAttributeMemory");

    public void GetObjectAttributes(int index, Span<byte> objectAttributeBuffer)
    {
        _buffer.AsSpan()
            .Slice(index * ObjectAttribute.Size, ObjectAttribute.Size)
            .CopyTo(objectAttributeBuffer);
    }
}
