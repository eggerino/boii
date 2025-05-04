namespace Boii.Graphics.Test;

public class TileTest
{
    [Fact]
    public void GetColorId()
    {
        Span<byte> tile = [0, 0, 0, 0, 0b0100_0000, 0b0100_0000, 0b1000_0000, 0, 0, 0b0010_0000, 0, 0, 0, 0, 0, 0];
        Assert.Equal(3, Tile.GetColorId(tile, 1, 2));
        Assert.Equal(1, Tile.GetColorId(tile, 0, 3));
        Assert.Equal(2, Tile.GetColorId(tile, 2, 4));
    }
}
