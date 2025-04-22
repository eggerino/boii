using Boii.Test.Mock;

namespace Boii.Processing.Test;

public class CpuTest
{
    [Fact]
    public void Nop()
    {
        var bus = Bus.From([0x00]);
        var cpu = Cpu.Create(bus);
        var expected = new Cpu.RegisterDump(0, 0, 0, 0, 0, 0x0101);

        Step(cpu, 1);

        AssertCpu(1, expected, cpu);
    }

    [Fact]
    public void LoadImm16()
    {
        var bus = Bus.From([
            0b0000_0001, 0x01, 0x02,
            0b0001_0001, 0x03, 0x04,
            0b0010_0001, 0x05, 0x06,
            0b0011_0001, 0x07, 0x08]);
        var cpu = Cpu.Create(bus);
        var expected = new Cpu.RegisterDump(0, 0x0201, 0x0403, 0x0605, 0x0807, 0x010C);

        Step(cpu, 4);

        AssertCpu(12, expected, cpu);
    }

    private static void Step(Cpu cpu, int amount)
    {
        foreach (var _ in Enumerable.Repeat(0, amount))
            cpu.Step();
    }

    private static void AssertCpu(ulong expectedTicks, Cpu.RegisterDump expectedDump, Cpu actual)
    {
        Assert.Equal(expectedTicks, actual.Ticks);
        Assert.Equal(expectedDump, actual.GetRegisterDump());
    }
}
