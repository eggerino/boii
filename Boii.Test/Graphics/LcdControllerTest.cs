using Boii.Abstractions;
using Boii.Memory;

namespace Boii.Graphics.Test;

public class LcdControllerTest
{
    [Fact]
    public void OmaDmaTransfer()
    {
        var bus = ArrayMemory.Create("", 0x1_0000);
        var controller = LcdController.Create(bus);

        foreach (var i in Enumerable.Range(0, 0x200))
        {
            bus.Write((ushort)i, (byte)i);
        }

        controller.Advance(1);
        controller.Advance(99);
        controller.Advance(100);

        AssertOam(Enumerable.Repeat(0, 0xA0), bus);

        controller.Write(6, 1);
        controller.Advance(10);
        controller.Advance(159);

        AssertOam(Enumerable.Repeat(0, 0xA0), bus);
        controller.Advance(1);
        AssertOam(Enumerable.Range(0, 0xA0), bus);
    }

    private static void AssertOam(IEnumerable<int> expected, IGenericIO bus)
    {
        foreach (var (x, i) in expected.Select((x, i) => (x, i)))
        {
            Assert.Equal(x, bus.Read((ushort)(0xFE00 + i)));
        }
    }

    [Fact]
    public void Control_IsWindowAndBackgroundEnabled()
    {
        var bus = ArrayMemory.Create("", 0);
        var controller = LcdController.Create(bus);
        Assert.False(controller.IsWindowAndBackgroudEnabled);
        controller.Write(0, 1);
        Assert.True(controller.IsWindowAndBackgroudEnabled);
    }

    [Fact]
    public void Control_AreObjectsEnabled()
    {
        var bus = ArrayMemory.Create("", 0);
        var controller = LcdController.Create(bus);
        Assert.False(controller.AreObjectsEnabled);
        controller.Write(0, 2);
        Assert.True(controller.AreObjectsEnabled);
    }

    [Fact]
    public void Control_ObjectSize()
    {
        var bus = ArrayMemory.Create("", 0);
        var controller = LcdController.Create(bus);
        Assert.Equal(ObjectSizeKind.Pixel8x8, controller.ObjectSize);
        controller.Write(0, 4);
        Assert.Equal(ObjectSizeKind.Pixel8x16, controller.ObjectSize);
    }

    [Fact]
    public void Control_BackgroundTileMapArea()
    {
        var bus = ArrayMemory.Create("", 0);
        var controller = LcdController.Create(bus);
        Assert.Equal(TileMapAreaKind.First, controller.BackgroundTileMapArea);
        controller.Write(0, 8);
        Assert.Equal(TileMapAreaKind.Second, controller.BackgroundTileMapArea);
    }

    [Fact]
    public void Control_WindowAndBackgroundTileArea()
    {
        var bus = ArrayMemory.Create("", 0);
        var controller = LcdController.Create(bus);
        Assert.Equal(TileDataAddressingMode.Block2, controller.WindowAndBackgroundTileArea);
        controller.Write(0, 16);
        Assert.Equal(TileDataAddressingMode.Block0, controller.WindowAndBackgroundTileArea);
    }

    [Fact]
    public void Control_IsWindowEnabled()
    {
        var bus = ArrayMemory.Create("", 0);
        var controller = LcdController.Create(bus);
        Assert.False(controller.IsWindowEnabled);
        controller.Write(0, 32);
        Assert.True(controller.IsWindowEnabled);
    }

    [Fact]
    public void Control_WindowTileMapArea()
    {
        var bus = ArrayMemory.Create("", 0);
        var controller = LcdController.Create(bus);
        Assert.Equal(TileMapAreaKind.First, controller.WindowTileMapArea);
        controller.Write(0, 64);
        Assert.Equal(TileMapAreaKind.Second, controller.WindowTileMapArea);
    }

    [Fact]
    public void Control_IsLcdAndPpuEnabled()
    {
        var bus = ArrayMemory.Create("", 0);
        var controller = LcdController.Create(bus);
        Assert.False(controller.IsLcdAndPpuEnabled);
        controller.Write(0, 128);
        Assert.True(controller.IsLcdAndPpuEnabled);
    }

    [Fact]
    public void Stat_SetPpuMode0()
    {
        var bus = ArrayMemory.Create("", 0x10000);
        var controller = LcdController.Create(bus);
        controller.Write(1, 8);
        controller.SetPpuMode(0);

        AssertStatRequested(bus);
        Assert.Equal(8, controller.Read(1));
    }

    [Fact]
    public void Stat_SetPpuMode1()
    {
        var bus = ArrayMemory.Create("", 0x10000);
        var controller = LcdController.Create(bus);
        controller.Write(1, 16);
        controller.SetPpuMode(1);

        AssertStatRequested(bus);
        Assert.Equal(17, controller.Read(1));
    }

    [Fact]
    public void Stat_SetPpuMode2()
    {
        var bus = ArrayMemory.Create("", 0x10000);
        var controller = LcdController.Create(bus);
        controller.Write(1, 32);
        controller.SetPpuMode(2);

        AssertStatRequested(bus);
        Assert.Equal(34, controller.Read(1));
    }

    [Fact]
    public void Stat_SetPpuMode3()
    {
        var bus = ArrayMemory.Create("", 0x10000);
        var controller = LcdController.Create(bus);
        controller.SetPpuMode(3);

        Assert.Equal(3, controller.Read(1));
    }

    private static void AssertStatRequested(IGenericIO bus)
    {
        Assert.Equal(2, bus.Read(0xFF0F));
    }

    [Fact]
    public void BackgroundViewportPositionY()
    {
        var bus = ArrayMemory.Create("", 0);
        var controller = LcdController.Create(bus);
        controller.Write(2, 69);
        Assert.Equal(69, controller.BackgroundViewportPositionY);
    }

    [Fact]
    public void BackgroundViewportPositionX()
    {
        var bus = ArrayMemory.Create("", 0);
        var controller = LcdController.Create(bus);
        controller.Write(3, 69);
        Assert.Equal(69, controller.BackgroundViewportPositionX);
    }

    [Fact]
    public void SetCurrentHorizontalLine()
    {
        var bus = ArrayMemory.Create("", 0x10000);
        var controller = LcdController.Create(bus);
        controller.Write(1, 64);
        controller.Write(5, 69);
        controller.SetCurrentHorizontalLine(69);

        AssertStatRequested(bus);
        Assert.Equal(69, controller.Read(4));
        Assert.Equal(68, controller.Read(1));
    }

    [Fact]
    public void GetColor()
    {
        var bus = ArrayMemory.Create("", 0);
        var controller = LcdController.Create(bus);
        controller.Write(7, 0b1110_0100);

        Assert.Equal(Color.White, controller.GetColor(0));
        Assert.Equal(Color.LightGray, controller.GetColor(1));
        Assert.Equal(Color.DarkGray, controller.GetColor(2));
        Assert.Equal(Color.Black, controller.GetColor(3));
    }

    [Fact]
    public void GetObjectColor0()
    {
        var bus = ArrayMemory.Create("", 0);
        var controller = LcdController.Create(bus);
        controller.Write(8, 0b1011_0001);

        Assert.Equal(Color.LightGray, controller.GetObjectColor0(0));
        Assert.Equal(Color.White, controller.GetObjectColor0(1));
        Assert.Equal(Color.Black, controller.GetObjectColor0(2));
        Assert.Equal(Color.DarkGray, controller.GetObjectColor0(3));
    }

    [Fact]
    public void GetObjectColor1()
    {
        var bus = ArrayMemory.Create("", 0);
        var controller = LcdController.Create(bus);
        controller.Write(9, 0b0011_10_01);

        Assert.Equal(Color.LightGray, controller.GetObjectColor1(0));
        Assert.Equal(Color.DarkGray, controller.GetObjectColor1(1));
        Assert.Equal(Color.Black, controller.GetObjectColor1(2));
        Assert.Equal(Color.White, controller.GetObjectColor1(3));
    }

    [Fact]
    public void WindowPositionY()
    {
        var bus = ArrayMemory.Create("", 0);
        var controller = LcdController.Create(bus);
        controller.Write(10, 69);
        Assert.Equal(69, controller.WindowPositionY);
    }

    [Fact]
    public void WindowPositionX()
    {
        var bus = ArrayMemory.Create("", 0);
        var controller = LcdController.Create(bus);
        controller.Write(11, 69);
        Assert.Equal(62, controller.WindowPositionX);
    }
}
