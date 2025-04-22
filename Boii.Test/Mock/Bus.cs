using Boii.Abstractions;

namespace Boii.Test.Mock;

public class Bus : IGenericIO
{
    private readonly List<byte> _data;

    private Bus(List<byte> data) => _data = data;

    public static Bus From(IEnumerable<byte> program)
    {
        var data = new List<byte>(0x0200);

        // Pad the boot rom
        data.AddRange(Enumerable.Repeat((byte)0, 0x0100));

        data.AddRange(program);

        return new(data);
    }

    public byte Read(ushort address) => _data[address];

    public void Write(ushort address, byte value) => _data[address] = value;
}
