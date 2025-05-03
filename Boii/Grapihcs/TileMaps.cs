using Boii.Abstractions;
using Boii.Memory;

namespace Boii.Graphics;

public class TileMaps : IGenericIO
{
    private readonly byte[] _buffer = new byte[0x0800];

    private TileMaps() { }

    public static TileMaps Create() => new();

    public byte Read(ushort address) => BufferAccesser.Read(_buffer, address, "VRAM.TileMaps");

    public void Write(ushort address, byte value) => BufferAccesser.Write(_buffer, address, value, "VRAM.TileMaps");

    public byte GetTileIndexFromFirst(int x, int y) => _buffer[32 * y + x];

    public byte GetTileIndexFromSecond(int x, int y) => _buffer[0x0400 + 32 * y + x];
}
