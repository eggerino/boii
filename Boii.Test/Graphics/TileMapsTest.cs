namespace Boii.Graphics.Test;

public class TileMapsTest
{
    [Fact]
    public void GetTileIndexFromFirst()
    {
        var tileMaps = TileMaps.Create();
        tileMaps.Write(32 * 3 + 2, 1);

        var actual = tileMaps.GetTileIndexFromFirst(2, 3);

        Assert.Equal(1, actual);
    }

    [Fact]
    public void GetTileIndexFromSecond()
    {
        var tileMaps = TileMaps.Create();
        tileMaps.Write(32 * 32 + 32 * 3 + 2, 1);

        var actual = tileMaps.GetTileIndexFromSecond(2, 3);

        Assert.Equal(1, actual);
    }
}