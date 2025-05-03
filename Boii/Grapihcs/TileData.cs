using System;
using Boii.Abstractions;
using Boii.Memory;

namespace Boii.Graphics;

public class TileData : IGenericIO
{
    private readonly byte[] _buffer = new byte[0x1800];

    private TileData() { }

    public static TileData Create() => new();

    public byte Read(ushort address) => BufferAccesser.Read(_buffer, address, "VRAM.TileData");

    public void Write(ushort address, byte value) => BufferAccesser.Write(_buffer, address, value, "VRAM.TileData");

    public void GetObjectTile(byte index, Span<byte> tileBuffer) => GetTile(index, tileBuffer);

    public void GetWindowOrBackgroundTileBlock0(byte index, Span<byte> tileBuffer) => GetTile(index, tileBuffer);
    
    public void GetWindowOrBackgroundTileBlock2(byte index, Span<byte> tileBuffer) => GetTile(index + 0x80, tileBuffer);

    private void GetTile(int tileIndex, Span<byte> tileBuffer)
    {
        _buffer.AsSpan()
            .Slice(tileIndex * Tile.Size, Tile.Size)
            .CopyTo(tileBuffer);
    }
}
