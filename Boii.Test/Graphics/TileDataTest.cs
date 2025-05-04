namespace Boii.Graphics.Test;

public class TileDataTest
{
    [Fact]
    public void GetObjectTile_Block0()
    {
        var tileData = TileData.Create();
        Span<byte> tile = stackalloc byte[Tile.Size];
        tileData.Write(4 * 16, 1);
        tileData.Write(4 * 16 + 15, 2);

        tileData.GetObjectTile(4, tile);

        Assert.Equal(1, tile[0]);
        Assert.Equal(2, tile[15]);
    }

    [Fact]
    public void GetObjectTile_Block1()
    {
        var tileData = TileData.Create();
        Span<byte> tile = stackalloc byte[Tile.Size];
        tileData.Write(200 * 16, 1);
        tileData.Write(200 * 16 + 15, 2);

        tileData.GetObjectTile(200, tile);

        Assert.Equal(1, tile[0]);
        Assert.Equal(2, tile[15]);
    }

    [Fact]
    public void GetWindowOrBackgroundTileBlock0_Block0()
    {
        var tileData = TileData.Create();
        Span<byte> tile = stackalloc byte[Tile.Size];
        tileData.Write(4 * 16, 1);
        tileData.Write(4 * 16 + 15, 2);

        tileData.GetWindowOrBackgroundTileBlock0(4, tile);

        Assert.Equal(1, tile[0]);
        Assert.Equal(2, tile[15]);
    }

    [Fact]
    public void GetWindowOrBackgroundTileBlock0_Block1()
    {
        var tileData = TileData.Create();
        Span<byte> tile = stackalloc byte[Tile.Size];
        tileData.Write(200 * 16, 1);
        tileData.Write(200 * 16 + 15, 2);

        tileData.GetWindowOrBackgroundTileBlock0(200, tile);

        Assert.Equal(1, tile[0]);
        Assert.Equal(2, tile[15]);
    }

    [Fact]
    public void GetWindowOrBackgroundTileBlock2_Block0()
    {
        var tileData = TileData.Create();
        Span<byte> tile = stackalloc byte[Tile.Size];
        tileData.Write(260 * 16, 1);
        tileData.Write(260 * 16 + 15, 2);

        tileData.GetWindowOrBackgroundTileBlock2(4, tile);

        Assert.Equal(1, tile[0]);
        Assert.Equal(2, tile[15]);
    }

    [Fact]
    public void GetWindowOrBackgroundTileBlock2_Block1()
    {
        var tileData = TileData.Create();
        Span<byte> tile = stackalloc byte[Tile.Size];
        tileData.Write(200 * 16, 1);
        tileData.Write(200 * 16 + 15, 2);

        tileData.GetWindowOrBackgroundTileBlock2(200, tile);

        Assert.Equal(1, tile[0]);
        Assert.Equal(2, tile[15]);
    }
}