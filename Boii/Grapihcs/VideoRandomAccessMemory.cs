using Boii.Abstractions;

namespace Boii.Graphics;

public class VideoRandomAccessMemory : IGenericIO
{
    public TileData TileData { get; }= TileData.Create();
    public TileMaps TileMaps { get; }= TileMaps.Create();

    private VideoRandomAccessMemory() { }

    public static VideoRandomAccessMemory Create() => new();

    public byte Read(ushort address) => address < 0x1800 ? TileData.Read(address) : TileMaps.Read((ushort)(address - 0x1800));

    public void Write(ushort address, byte value)
    {
        if (address < 0x1800) TileData.Write(address, value);
        else TileMaps.Write((ushort)(address - 0x1800), value);
    }
}
