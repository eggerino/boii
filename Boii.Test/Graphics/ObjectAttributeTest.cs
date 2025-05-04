namespace Boii.Graphics.Test;

public class ObjectAttributeTest
{
    [Theory]
    [InlineData(0, -16)]
    [InlineData(16, 0)]
    [InlineData(255, 239)]
    public void GetYPosition(int inBuffer, int expected)
    {
        Span<byte> om = [(byte)inBuffer, 0, 0, 0];
        var actual = ObjectAttribute.GetYPosition(om);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0, -8)]
    [InlineData(8, 0)]
    [InlineData(255, 247)]
    public void GetXPosition(int inBuffer, int expected)
    {
        Span<byte> om = [0, (byte)inBuffer, 0, 0];
        var actual = ObjectAttribute.GetXPosition(om);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetSingleTileIndex()
    {
        Span<byte> om = [0, 0, 69, 0];
        var actual = ObjectAttribute.GetSingleTileIndex(om);
        Assert.Equal(69, actual);
    }

    [Fact]
    public void GetDoubleTilesIndex_Even()
    {
        Span<byte> om = [0, 0, 68, 0];
        var actual = ObjectAttribute.GetDoubleTilesIndex(om);
        Assert.Equal(68, actual.top);
        Assert.Equal(69, actual.bottom);
    }

    [Fact]
    public void GetDoubleTilesIndex_Uneven()
    {
        Span<byte> om = [0, 0, 69, 0];
        var actual = ObjectAttribute.GetDoubleTilesIndex(om);
        Assert.Equal(68, actual.top);
        Assert.Equal(69, actual.bottom);
    }

    [Fact]
    public void GetPriorityFlag()
    {
        Span<byte> om = [0, 0, 0, 0b1000_0000];
        var actual = ObjectAttribute.GetPriorityFlag(om);
        Assert.True(actual);
    }

    [Fact]
    public void GetYFlipFlag()
    {
        Span<byte> om = [0, 0, 0, 0b0100_0000];
        var actual = ObjectAttribute.GetYFlipFlag(om);
        Assert.True(actual);
    }

    [Fact]
    public void GetXFlipFlag()
    {
        Span<byte> om = [0, 0, 0, 0b0010_0000];
        var actual = ObjectAttribute.GetXFlipFlag(om);
        Assert.True(actual);
    }

    [Fact]
    public void GetDmgPalletFlag()
    {
        Span<byte> om = [0, 0, 0, 0b0001_0000];
        var actual = ObjectAttribute.GetDmgPalletFlag(om);
        Assert.True(actual);
    }
}
