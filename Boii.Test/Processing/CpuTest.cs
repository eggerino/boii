using Boii.Test.Mock;

namespace Boii.Processing.Test;

public class CpuTest
{
    [Fact]
    public void Nop()
    {
        var bus = Bus.From([0x00]); // nop
        var cpu = Cpu.Create(bus);

        Step(cpu, 1);

        AssertCpu(1, new(0, 0, 0, 0, 0, 0x0101), cpu);
    }

    [Fact]
    public void LoadImm8()
    {
        var bus = Bus.From([
            0b0011_0110, 0xFF,  // ld [hl], 256 (ld [0], 256)
            0b0000_0110, 0x01,  // ld b, 1
            0b0000_1110, 0x02,  // ld c, 2
            0b0001_0110, 0x03,  // ld d, 3
            0b0001_1110, 0x04,  // ld e, 4
            0b0010_0110, 0x05,  // ld h, 5
            0b0010_1110, 0x06,  // ld l, 6
            0b0011_1110, 0x08,  // ld a, 8
        ]);
        var cpu = Cpu.Create(bus);

        Step(cpu, 8);

        AssertCpu(17, new(0x0800, 0x0102, 0x0304, 0x0506, 0x0000, 0x0110), cpu);
        Assert.Equal(0xFF, bus.Read(0x0000));
    }

    [Fact]
    public void LoadImm16()
    {
        var bus = Bus.From([
            0b0000_0001, 0x01, 0x02,    // ld bc, 0x0201
            0b0001_0001, 0x03, 0x04,    // ld de, 0x0403
            0b0010_0001, 0x05, 0x06,    // ld hl, 0x0605
            0b0011_0001, 0x07, 0x08     // ld sp, 0x0807
        ]);
        var cpu = Cpu.Create(bus);

        Step(cpu, 4);

        AssertCpu(12, new(0, 0x0201, 0x0403, 0x0605, 0x0807, 0x010C), cpu);
    }

    [Fact]
    public void LoadFromA()
    {
        var bus = Bus.From([
            0b0011_1110, 0xFF,  // ld a, 256
            0b0000_1110, 0x05,  // ld c, 5
            0b0001_1110, 0x06,  // ld e, 6
            0b0000_0010,        // ld [bc], a
            0b0001_0010,        // ld [de], a
            0b0010_0010,        // ld [hl+], a
            0b0011_0010,        // ld [hl-], a
        ]);
        var cpu = Cpu.Create(bus);

        Step(cpu, 7);

        AssertCpu(14, new(0xFF00, 0x0005, 0x0006, 0x0000, 0, 0x010A), cpu);
        Assert.Equal(0xFF, bus.Read(0x0005));
        Assert.Equal(0xFF, bus.Read(0x0006));
        Assert.Equal(0xFF, bus.Read(0x0000));
        Assert.Equal(0xFF, bus.Read(0x0001));
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
