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

    [Fact]
    public void LoadIntoA()
    {
        var bus = Bus.From([
            0b0000_0001, 0x01, 0x00,    // ld bc, 1
            0b0001_0001, 0x02, 0x00,    // ld de, 2
            0b0010_0001, 0x03, 0x00,    // ld hl, 3
            0b0000_1010,                // ld a, [bc]
            0b0001_1010,                // ld a, [de]
            0b0010_1010,                // ld a, [hl+]
            0b0011_1010,                // ld a, [hl-]
        ]);
        bus.Write(1, 1);
        bus.Write(2, 2);
        bus.Write(3, 3);
        bus.Write(4, 4);

        var cpu = Cpu.Create(bus);

        Step(cpu, 4); ;
        AssertCpu(11, new(0x0100, 0x0001, 0x0002, 0x0003, 0, 0x010A), cpu);
        cpu.Step();
        AssertCpu(13, new(0x0200, 0x0001, 0x0002, 0x0003, 0, 0x010B), cpu);
        cpu.Step();
        AssertCpu(15, new(0x0300, 0x0001, 0x0002, 0x0004, 0, 0x010C), cpu);
        cpu.Step();
        AssertCpu(17, new(0x0400, 0x0001, 0x0002, 0x0003, 0, 0x010D), cpu);
    }

    [Fact]
    public void LoadFromStackPointer()
    {
        var bus = Bus.From([
            0b0011_0001, 0x07, 0x08,    // ld sp, 0x0807
            0b0000_1000, 0x01, 0x00     // ld [0x0001], sp
        ]);
        var cpu = Cpu.Create(bus);

        Step(cpu, 2);

        AssertCpu(8, new(0, 0, 0, 0, 0x0807, 0x0106), cpu);
        Assert.Equal(0x07, bus.Read(1));
        Assert.Equal(0x08, bus.Read(2));
    }

    [Fact]
    public void IncrementRegister16()
    {
        var bus = Bus.From([
            0b0000_0011,                // inc bc
            0b0001_0011,                // inc de
            0b0010_0011,                // inc hl
            0b0011_0011,                // inc sp
        ]);
        var cpu = Cpu.Create(bus);

        Step(cpu, 4);

        AssertCpu(8, new(0, 1, 1, 1, 1, 0x0104), cpu);
    }

    [Fact]
    public void DecrementRegister16()
    {
        var bus = Bus.From([
            0b0000_1011,                // dec bc
            0b0001_1011,                // dec de
            0b0010_1011,                // dec hl
            0b0011_1011,                // dec sp
        ]);
        var cpu = Cpu.Create(bus);

        Step(cpu, 4);

        AssertCpu(8, new(0, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0x0104), cpu);
    }

    [Fact]
    public void AddRegister16ToHL()
    {
        var bus = Bus.From([
            0b0000_0001, 0xFF, 0x0F,    // ld bc, 0x0FFF
            0b0001_0001, 0x01, 0x00,    // ld de, 0x0001
            0b0011_0001, 0x01, 0xE0,    // ld sp, 0xE001
            0b0000_1001,                // dec bc
            0b0001_1001,                // dec de
            0b0010_1001,                // dec hl
            0b0011_1001,                // dec sp
        ]);
        var cpu = Cpu.Create(bus);

        Step(cpu, 4);
        AssertCpu(11, new(0b0000_0000_0000_0000, 0x0FFF, 0x0001, 0x0FFF, 0xE001, 0x010A), cpu);
        cpu.Step();
        AssertCpu(13, new(0b0000_0000_0010_0000, 0x0FFF, 0x0001, 0x1000, 0xE001, 0x010B), cpu);
        cpu.Step();
        AssertCpu(15, new(0b0000_0000_0010_0000, 0x0FFF, 0x0001, 0x2000, 0xE001, 0x010C), cpu);
        cpu.Step();
        AssertCpu(17, new(0b0000_0000_0011_0000, 0x0FFF, 0x0001, 0x0001, 0xE001, 0x010D), cpu);
    }

    [Fact]
    public void IncrementRegister8()
    {
        var bus = Bus.From([
            0b0000_0110, 0xFF,  // ld b, 255
            0b0000_1110, 0x0F,  // ld c, 16
            0b0000_0100,        // inc b
            0b0000_1100,        // inc c
            0b0001_0100,        // inc d
            0b0001_1100,        // inc e
            0b0011_0100,        // inc [hl]
            0b0010_0100,        // inc h
            0b0010_1100,        // inc l
            0b0011_1100,        // inc a
        ]);
        var cpu = Cpu.Create(bus);

        Step(cpu, 10);

        AssertCpu(14, new(0x0100 | 0b1010_0000, 0x0010, 0x0101, 0x0101, 0x0000, 0x010C), cpu);
        Assert.Equal(0x01, bus.Read(0x0000));
    }

    [Fact]
    public void DecrementRegister8()
    {
        var bus = Bus.From([
            0b0000_0110, 0x01,  // ld b, 1
            0b0000_0101,        // dec b
            0b0000_1101,        // dec c
            0b0001_0101,        // dec d
            0b0001_1101,        // dec e
            0b0011_0101,        // dec [hl]
            0b0010_0101,        // dec h
            0b0010_1101,        // dec l
            0b0011_1101,        // dec a
        ]);
        var cpu = Cpu.Create(bus);

        Step(cpu, 9);

        AssertCpu(12, new(0xFF00 | 0b1110_0000, 0x00FF, 0xFFFF, 0xFFFF, 0x0000, 0x010A), cpu);
        Assert.Equal(0xFF, bus.Read(0x0000));
    }

    [Fact]
    public void RotateLeftA_WithAndWithoutCarry()
    {
        var bus = Bus.From([
            0b0011_1110, 0b1000_1110,   // ld a, 0b1000_1110
            0b0000_0111,                // rlca
            0b0001_0111                 // rla
        ]);
        var cpu = Cpu.Create(bus);

        Step(cpu, 2);
        AssertCpu(3, new(0b0001_1101_0001_0000, 0, 0, 0, 0, 0x0103), cpu);
        cpu.Step();
        AssertCpu(4, new(0b0011_1011_0000_0000, 0, 0, 0, 0, 0x0104), cpu);
    }

    [Fact]
    public void RotateRightA_WithAndWithoutCarry()
    {
        var bus = Bus.From([
            0b0011_1110, 0b0111_0001,   // ld a, 0b0111_0001
            0b0000_1111,                // rrca
            0b0001_1111                 // rra
        ]);
        var cpu = Cpu.Create(bus);

        Step(cpu, 2);
        AssertCpu(3, new(0b1011_1000_0001_0000, 0, 0, 0, 0, 0x0103), cpu);
        cpu.Step();
        AssertCpu(4, new(0b1101_1100_0000_0000, 0, 0, 0, 0, 0x0104), cpu);
    }

    [Theory]
    // Subtraction cases
    [InlineData(0x6600 | 0b0100_0000, 0x6600 | 0b0100_0000)]
    [InlineData(0x6600 | 0b0110_0000, 0x6000 | 0b0100_0000)]
    [InlineData(0x6600 | 0b0101_0000, 0x0600 | 0b0100_0000)]
    [InlineData(0x6600 | 0b0111_0000, 0x0000 | 0b1100_0000)]
    // Addition cases
    [InlineData(0x0000 | 0b0000_0000, 0x0000 | 0b1000_0000)]
    [InlineData(0x0000 | 0b0010_0000, 0x0600 | 0b0000_0000)]
    [InlineData(0x0A00 | 0b0000_0000, 0x1000 | 0b0000_0000)]
    [InlineData(0x0000 | 0b0001_0000, 0x6000 | 0b0001_0000)]
    [InlineData(0xA000 | 0b0000_0000, 0x0000 | 0b1001_0000)]
    [InlineData(0x0000 | 0b0011_0000, 0x6600 | 0b0001_0000)]
    public void DecimalAdjustAccumulator(ushort initialAF, ushort expectedAF)
    {
        var bus = Bus.From([
            0b0010_0111         // daa
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(initialAF, 0, 0, 0, 0, 0x0100));

        cpu.Step();

        AssertCpu(1, new(expectedAF, 0, 0, 0, 0, 0x0101), cpu);
    }

    [Fact]
    public void ComplementAccumulator()
    {
        var bus = Bus.From([
            0b0010_1111         // cpl
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0b1010_1010_0000_0000, 0, 0, 0, 0, 0x0100));

        cpu.Step();

        AssertCpu(1, new(0b0101_0101_0110_0000, 0, 0, 0, 0, 0x0101), cpu);
    }

    [Fact]
    public void SetCarryFlag()
    {
        var bus = Bus.From([
            0b0011_0111         // scf
        ]);
        var cpu = Cpu.Create(bus);

        cpu.Step();

        AssertCpu(1, new(0x0000 | 0b0001_0000, 0, 0, 0, 0, 0x0101), cpu);
    }

    [Fact]
    public void ComplementCarryFlag()
    {
        var bus = Bus.From([
            0b0011_1111         // ccf
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0x0000 | 0b0001_0000, 0, 0, 0, 0, 0x0100));

        cpu.Step();

        AssertCpu(1, new(0x0000 | 0b0000_0000, 0, 0, 0, 0, 0x0101), cpu);
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
