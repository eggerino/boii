using Boii.Abstractions;

namespace Boii.Graphics;

public class VideoRandomAccessMemory : IGenericIO
{
    private readonly TileData _tileData = TileData.Create();
    private readonly TileMaps _tileMaps = TileMaps.Create();

    private VideoRandomAccessMemory() { }

    public static VideoRandomAccessMemory Create() => new();

    public byte Read(ushort address) => address < 0x1800 ? _tileData.Read(address) : _tileMaps.Read((ushort)(address - 0x1800));

    public void Write(ushort address, byte value)
    {
        if (address < 0x1800) _tileData.Write(address, value);
        else _tileMaps.Write((ushort)(address - 0x1800), value);
    }
}
