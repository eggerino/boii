namespace Boii.Graphics.Test;

public class ObjectAttributeMemoryTest
{
    [Fact]
    public void GetObjectAttributes()
    {
        var oam = ObjectAttributeMemory.Create();
        Span<byte> oa = [0, 0, 0, 0];
        oam.Write(20, 1);
        oam.Write(21, 2);
        oam.Write(22, 3);
        oam.Write(23, 4);

        oam.GetObjectAttributes(5, oa);

        Assert.Equal(1, oa[0]);
        Assert.Equal(2, oa[1]);
        Assert.Equal(3, oa[2]);
        Assert.Equal(4, oa[3]);
    }
}