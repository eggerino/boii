namespace Boii.Processing.Test;

public class CpuRegistersTest
{
    [Fact]
    public void AFGetter()
    {
        var reg = CpuRegisters.Create();
        reg.A = 2;
        reg.F = 1;

        Assert.Equal(0x0201, reg.AF);
    }

    [Fact]
    public void BCGetter()
    {
        var reg = CpuRegisters.Create();
        reg.B = 2;
        reg.C = 1;

        Assert.Equal(0x0201, reg.BC);
    }

    [Fact]
    public void DEGetter()
    {
        var reg = CpuRegisters.Create();
        reg.D = 2;
        reg.E = 1;

        Assert.Equal(0x0201, reg.DE);
    }

    [Fact]
    public void HLGetter()
    {
        var reg = CpuRegisters.Create();
        reg.H = 2;
        reg.L = 1;

        Assert.Equal(0x0201, reg.HL);
    }

    [Fact]
    public void AFSetter()
    {
        var reg = CpuRegisters.Create();
        reg.AF = 0x0201;

        Assert.Equal(2, reg.A);
        Assert.Equal(1, reg.F);
    }

    [Fact]
    public void BCSetter()
    {
        var reg = CpuRegisters.Create();
        reg.BC = 0x0201;

        Assert.Equal(2, reg.B);
        Assert.Equal(1, reg.C);
    }

    [Fact]
    public void DESetter()
    {
        var reg = CpuRegisters.Create();
        reg.DE = 0x0201;

        Assert.Equal(2, reg.D);
        Assert.Equal(1, reg.E);
    }

    [Fact]
    public void HLSetter()
    {
        var reg = CpuRegisters.Create();
        reg.HL = 0x0201;

        Assert.Equal(2, reg.H);
        Assert.Equal(1, reg.L);
    }

    [Fact]
    public void ZeroGetter()
    {
        var reg = CpuRegisters.Create();
        reg.F = 0b1000_0000;

        Assert.True(reg.Zero);
    }

    [Fact]
    public void SubtractionGetter()
    {
        var reg = CpuRegisters.Create();
        reg.F = 0b0100_0000;

        Assert.True(reg.Subtraction);
    }

    [Fact]
    public void HalfCarryGetter()
    {
        var reg = CpuRegisters.Create();
        reg.F = 0b0010_0000;

        Assert.True(reg.HalfCarry);
    }

    [Fact]
    public void CarryGetter()
    {
        var reg = CpuRegisters.Create();
        reg.F = 0b0001_0000;

        Assert.True(reg.Carry);
    }

    [Fact]
    public void ZeroSetter()
    {
        var reg = CpuRegisters.Create();
        reg.Zero = true;

        Assert.Equal(0b1000_0000, reg.F);
    }

    [Fact]
    public void SubtractionSetter()
    {
        var reg = CpuRegisters.Create();
        reg.Subtraction = true;

        Assert.Equal(0b0100_0000, reg.F);
    }

    [Fact]
    public void HalfCarrySetter()
    {
        var reg = CpuRegisters.Create();
        reg.HalfCarry = true;

        Assert.Equal(0b0010_0000, reg.F);
    }

    [Fact]
    public void CarrySetter()
    {
        var reg = CpuRegisters.Create();
        reg.Carry = true;

        Assert.Equal(0b0001_0000, reg.F);
    }
}
