using Boii.Test.Mock;

namespace Boii.Processing.Test;

public class CpuTest
{
    // Misc
    [Fact]
    public void Nop()
    {
        var bus = Bus.From([0x00]); // nop
        var cpu = Cpu.Create(bus);

        Step(cpu, 1);

        AssertCpu(1, new(0, 0, 0, 0, 0, 0x0101), cpu);
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

    // Interrupt
    [Fact]
    public void EnableInterrupt()
    {
        var bus = Bus.From([
            0b1111_1011,        // ei
            0                   // nop
        ]);
        var cpu = Cpu.Create(bus);

        cpu.Step();
        AssertCpu(1, new(0, 0, 0, 0, 0, 0x0101, Interrupt: false), cpu);
        cpu.Step();
        AssertCpu(2, new(0, 0, 0, 0, 0, 0x0102, Interrupt: true), cpu);
    }

    [Fact]
    public void DisableInterrupt()
    {
        var bus = Bus.From([
            0b1111_0011         // di
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0, 0, 0, 0, 0x0100, Interrupt: true));

        cpu.Step();
        AssertCpu(1, new(0, 0, 0, 0, 0, 0x0101, Interrupt: false), cpu);
    }

    [Fact]
    public void EnableAndDisableInterrupt()
    {
        var bus = Bus.From([
            0b1111_1011,        // ei
            0b1111_0011,        // di
            0,                  // nop
            0,                  // nop
            0b1111_1011,        // ei
            0,                  // nop
            0,                  // nop
            0b1111_0011,        // di
            0,                  // nop
            0,                  // nop
        ]);
        var cpu = Cpu.Create(bus);

        cpu.Step();
        AssertCpu(1, new(0, 0, 0, 0, 0, 0x0101, Interrupt: false), cpu);
        cpu.Step();
        AssertCpu(2, new(0, 0, 0, 0, 0, 0x0102, Interrupt: false), cpu);
        cpu.Step();
        AssertCpu(3, new(0, 0, 0, 0, 0, 0x0103, Interrupt: false), cpu);
        cpu.Step();
        AssertCpu(4, new(0, 0, 0, 0, 0, 0x0104, Interrupt: false), cpu);
        cpu.Step();
        AssertCpu(5, new(0, 0, 0, 0, 0, 0x0105, Interrupt: false), cpu);
        cpu.Step();
        AssertCpu(6, new(0, 0, 0, 0, 0, 0x0106, Interrupt: true), cpu);
        cpu.Step();
        AssertCpu(7, new(0, 0, 0, 0, 0, 0x0107, Interrupt: true), cpu);
        cpu.Step();
        AssertCpu(8, new(0, 0, 0, 0, 0, 0x0108, Interrupt: false), cpu);
        cpu.Step();
        AssertCpu(9, new(0, 0, 0, 0, 0, 0x0109, Interrupt: false), cpu);
        cpu.Step();
        AssertCpu(10, new(0, 0, 0, 0, 0, 0x010A, Interrupt: false), cpu);
    }

    // Load
    [Fact]
    public void LoadLiteral8()
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
    public void LoadRegister8ToRegister8_FromB()
    {
        var bus = Bus.From([
            0b0111_0000,        // ld [hl], b
            0b0100_0000,        // ld b, b
            0b0100_1000,        // ld c, b
            0b0101_0000,        // ld d, b
            0b0101_1000,        // ld e, b
            0b0110_0000,        // ld h, b
            0b0110_1000,        // ld l, b
            0b0111_1000,        // ld a, b
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0x0100, 0, 0, 0, 0x0100));

        Step(cpu, 8);

        AssertCpu(9, new(0x0100, 0x0101, 0x0101, 0x0101, 0, 0x0108), cpu);
        Assert.Equal(1, bus.Read(0));
    }

    [Fact]
    public void LoadRegister8ToRegister8_IntoB()
    {
        var bus = Bus.From([
            // 0b0100_0110,        // ld b, [hl]
            0b0100_0000,        // ld b, b
            0b0100_0001,        // ld b, c
            0b0100_0010,        // ld b, d
            0b0100_0011,        // ld b, e
            0b0100_0100,        // ld b, h
            0b0100_0101,        // ld b, l
            0b0100_0111,        // ld b, a
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0x0600, 0x001, 0x0203, 0x0405, 0, 0x0100));

        cpu.Step();
        AssertCpu(1, new(0x0600, 0x0001, 0x0203, 0x0405, 0, 0x0101), cpu);
        cpu.Step();
        AssertCpu(2, new(0x0600, 0x0101, 0x0203, 0x0405, 0, 0x0102), cpu);
        cpu.Step();
        AssertCpu(3, new(0x0600, 0x0201, 0x0203, 0x0405, 0, 0x0103), cpu);
        cpu.Step();
        AssertCpu(4, new(0x0600, 0x0301, 0x0203, 0x0405, 0, 0x0104), cpu);
        cpu.Step();
        AssertCpu(5, new(0x0600, 0x0401, 0x0203, 0x0405, 0, 0x0105), cpu);
        cpu.Step();
        AssertCpu(6, new(0x0600, 0x0501, 0x0203, 0x0405, 0, 0x0106), cpu);
        cpu.Step();
        AssertCpu(7, new(0x0600, 0x0601, 0x0203, 0x0405, 0, 0x0107), cpu);
    }

    [Fact]
    public void LoadRegister8ToRegister8_HLPointerIntoB()
    {
        var bus = Bus.From([
            0b0100_0110         // ld b, [hl]
        ]);
        bus.Write(0, 1);
        var cpu = Cpu.Create(bus);

        cpu.Step();
        AssertCpu(2, new(0, 0x0100, 0, 0, 0, 0x0101), cpu);
    }

    [Fact]
    public void LoadLiteral16()
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
            0b0000_0010,        // ld [bc], a
            0b0001_0010,        // ld [de], a
            0b0010_0010,        // ld [hl+], a
            0b0011_0010,        // ld [hl-], a
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0xFF00, 0x0005, 0x0006, 0, 0, 0x0100));

        Step(cpu, 4);

        AssertCpu(8, new(0xFF00, 0x0005, 0x0006, 0x0000, 0, 0x0104), cpu);
        Assert.Equal(0xFF, bus.Read(0x0005));
        Assert.Equal(0xFF, bus.Read(0x0006));
        Assert.Equal(0xFF, bus.Read(0x0000));
        Assert.Equal(0xFF, bus.Read(0x0001));
    }

    [Fact]
    public void LoadFromAIntoLiteral16Pointer()
    {
        var bus = Bus.From([0b1110_1010, 0x05, 0x00]);  // ld [5], a
        var cpu = Cpu.CreateWithRegisterState(bus, new(0x0400, 0, 0, 0, 0, 0x0100));

        cpu.Step();

        AssertCpu(4, new(0x0400, 0, 0, 0, 0, 0x0103), cpu);
        Assert.Equal(4, bus.Read(0x5));
    }

    [Fact]
    public void LoadFromAIntoLiteral8HighPointer()
    {
        var bus = Bus.From([0b1110_0000, 0x01]);  // ldh [1], a
        bus.EnsureSize(0xFF02);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0x0400, 0, 0, 0, 0, 0x0100));

        cpu.Step();

        AssertCpu(3, new(0x0400, 0, 0, 0, 0, 0x0102), cpu);
        Assert.Equal(4, bus.Read(0xFF01));
    }

    [Fact]
    public void LoadFromAIntoCHighPointer()
    {
        var bus = Bus.From([0b1110_0010]);  // ldh [c], a
        bus.EnsureSize(0xFF02);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0x0400, 0x0001, 0, 0, 0, 0x0100));

        cpu.Step();

        AssertCpu(2, new(0x0400, 0x0001, 0, 0, 0, 0x0101), cpu);
        Assert.Equal(4, bus.Read(0xFF01));
    }

    [Fact]
    public void LoadIntoA()
    {
        var bus = Bus.From([
            0b0000_1010,                // ld a, [bc]
            0b0001_1010,                // ld a, [de]
            0b0010_1010,                // ld a, [hl+]
            0b0011_1010,                // ld a, [hl-]
        ]);
        bus.Write(1, 1);
        bus.Write(2, 2);
        bus.Write(3, 3);
        bus.Write(4, 4);

        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 1, 2, 3, 0, 0x0100));

        cpu.Step();
        AssertCpu(2, new(0x0100, 0x0001, 0x0002, 0x0003, 0, 0x0101), cpu);
        cpu.Step();
        AssertCpu(4, new(0x0200, 0x0001, 0x0002, 0x0003, 0, 0x0102), cpu);
        cpu.Step();
        AssertCpu(6, new(0x0300, 0x0001, 0x0002, 0x0004, 0, 0x0103), cpu);
        cpu.Step();
        AssertCpu(8, new(0x0400, 0x0001, 0x0002, 0x0003, 0, 0x0104), cpu);
    }

    [Fact]
    public void LoadFromLiteral16PointerIntoA()
    {
        var bus = Bus.From([0b1111_1010, 0x05, 0x00]);  // ld [5], a
        bus.Write(0x5, 4);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0, 0, 0, 0, 0x0100));

        cpu.Step();

        AssertCpu(4, new(0x0400, 0, 0, 0, 0, 0x0103), cpu);
    }

    [Fact]
    public void LoadFromLiteral8HighPointerIntoA()
    {
        var bus = Bus.From([0b1111_0000, 0x01]);  // ldh [1], a
        bus.EnsureSize(0xFF02);
        bus.Write(0xFF01, 4);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0, 0, 0, 0, 0x0100));

        cpu.Step();

        AssertCpu(3, new(0x0400, 0, 0, 0, 0, 0x0102), cpu);
    }

    [Fact]
    public void LoadFromCHighPointerIntoA()
    {
        var bus = Bus.From([0b1111_0010]);  // ldh a, [c]
        bus.EnsureSize(0xFF02);
        bus.Write(0xFF01, 4);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0x0001, 0, 0, 0, 0x0100));

        cpu.Step();

        AssertCpu(2, new(0x0400, 0x0001, 0, 0, 0, 0x0101), cpu);
    }

    // 8 Bit arithmetic
    [Fact]
    public void IncrementRegister8()
    {
        var bus = Bus.From([
            0b0000_0100,        // inc b
            0b0000_1100,        // inc c
            0b0001_0100,        // inc d
            0b0001_1100,        // inc e
            0b0011_0100,        // inc [hl]
            0b0010_0100,        // inc h
            0b0010_1100,        // inc l
            0b0011_1100,        // inc a
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0xFF0F, 0, 0, 0, 0x0100));

        cpu.Step();
        AssertCpu(1, new(0x0000 | 0b1010_0000, 0x000F, 0, 0, 0, 0x0101), cpu);
        cpu.Step();
        AssertCpu(2, new(0x0000 | 0b0010_0000, 0x0010, 0, 0, 0, 0x0102), cpu);
        cpu.Step();
        AssertCpu(3, new(0x0000 | 0b0000_0000, 0x0010, 0x0100, 0, 0, 0x0103), cpu);
        cpu.Step();
        AssertCpu(4, new(0x0000 | 0b0000_0000, 0x0010, 0x0101, 0, 0, 0x0104), cpu);
        cpu.Step();
        AssertCpu(7, new(0x0000 | 0b0000_0000, 0x0010, 0x0101, 0, 0, 0x0105), cpu);
        Assert.Equal(0x01, bus.Read(0x0000));
        cpu.Step();
        AssertCpu(8, new(0x0000 | 0b0000_0000, 0x0010, 0x0101, 0x0100, 0, 0x0106), cpu);
        cpu.Step();
        AssertCpu(9, new(0x0000 | 0b0000_0000, 0x0010, 0x0101, 0x0101, 0, 0x0107), cpu);
        cpu.Step();
        AssertCpu(10, new(0x0100 | 0b0000_0000, 0x0010, 0x0101, 0x0101, 0, 0x0108), cpu);
    }

    [Fact]
    public void DecrementRegister8()
    {
        var bus = Bus.From([
            0b0000_0101,        // dec b
            0b0000_1101,        // dec c
            0b0001_0101,        // dec d
            0b0001_1101,        // dec e
            0b0011_0101,        // dec [hl]
            0b0010_0101,        // dec h
            0b0010_1101,        // dec l
            0b0011_1101,        // dec a
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0x0102, 0, 0, 0, 0x0100));

        cpu.Step();
        AssertCpu(1, new(0x0000 | 0b1100_0000, 0x0002, 0, 0, 0, 0x0101), cpu);
        cpu.Step();
        AssertCpu(2, new(0x0000 | 0b0100_0000, 0x0001, 0, 0, 0, 0x0102), cpu);
        cpu.Step();
        AssertCpu(3, new(0x0000 | 0b0110_0000, 0x0001, 0xFF00, 0, 0, 0x0103), cpu);
        cpu.Step();
        AssertCpu(4, new(0x0000 | 0b0110_0000, 0x0001, 0xFFFF, 0, 0, 0x0104), cpu);
        cpu.Step();
        AssertCpu(7, new(0x0000 | 0b0110_0000, 0x0001, 0xFFFF, 0, 0, 0x0105), cpu);
        Assert.Equal(0xFF, bus.Read(0x0000));
        cpu.Step();
        AssertCpu(8, new(0x0000 | 0b0110_0000, 0x0001, 0xFFFF, 0xFF00, 0, 0x0106), cpu);
        cpu.Step();
        AssertCpu(9, new(0x0000 | 0b0110_0000, 0x0001, 0xFFFF, 0xFFFF, 0, 0x0107), cpu);
        cpu.Step();
        AssertCpu(10, new(0xFF00 | 0b0110_0000, 0x0001, 0xFFFF, 0xFFFF, 0, 0x0108), cpu);
    }

    [Fact]
    public void AddToA()
    {
        var bus = Bus.From([
            0b1000_0000,        // add a, b
            0b1000_0001,        // add a, c
            0b1000_0010,        // add a, d
            0b1000_0011,        // add a, e
            0b1000_0100,        // add a, h
            0b1000_0101,        // add a, l
            0b1000_0110,        // add a, [hl]
            0b1000_0111,        // add a, a
        ]);
        bus.Write(0, 0x10);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0x0001, 0x0FF3, 0x0000, 0, 0x0100));

        cpu.Step();
        AssertCpu(1, new(0x0000 | 0b1000_0000, 0x0001, 0x0FF3, 0x0000, 0, 0x0101), cpu);
        cpu.Step();
        AssertCpu(2, new(0x0100 | 0b0000_0000, 0x0001, 0x0FF3, 0x0000, 0, 0x0102), cpu);
        cpu.Step();
        AssertCpu(3, new(0x1000 | 0b0010_0000, 0x0001, 0x0FF3, 0x0000, 0, 0x0103), cpu);
        cpu.Step();
        AssertCpu(4, new(0x0300 | 0b0001_0000, 0x0001, 0x0FF3, 0x0000, 0, 0x0104), cpu);
        cpu.Step();
        AssertCpu(5, new(0x0300 | 0b0000_0000, 0x0001, 0x0FF3, 0x0000, 0, 0x0105), cpu);
        cpu.Step();
        AssertCpu(6, new(0x0300 | 0b0000_0000, 0x0001, 0x0FF3, 0x0000, 0, 0x0106), cpu);
        cpu.Step();
        AssertCpu(8, new(0x1300 | 0b0000_0000, 0x0001, 0x0FF3, 0x0000, 0, 0x0107), cpu);
        cpu.Step();
        AssertCpu(9, new(0x2600 | 0b0000_0000, 0x0001, 0x0FF3, 0x0000, 0, 0x0108), cpu);
    }

    [Fact]
    public void AddLiteral8ToA()
    {
        var bus = Bus.From([
            0b1100_0110, 0x10   // add a, 0x10
        ]);
        var cpu = Cpu.Create(bus);

        cpu.Step();
        AssertCpu(2, new(0x1000 | 0b0000_0000, 0, 0, 0, 0, 0x0102), cpu);
    }

    [Fact]
    public void AddToACarry()
    {
        var bus = Bus.From([
            0b1000_1000,        // adc a, b
            0b1000_1001         // adc a, c
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0xFF00, 0x0101, 0, 0, 0, 0x0100));

        Step(cpu, 2);
        AssertCpu(2, new(0x0200 | 0b0000_0000, 0x0101, 0, 0, 0, 0x0102), cpu);
    }

    [Fact]
    public void AddLiteral8ToACarry()
    {
        var bus = Bus.From([
            0b1100_1110, 0x01,  // adc a, 0x01
            0b1100_1110, 0x01,  // adc a, 0x01
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0xFF00, 0, 0, 0, 0, 0x0100));

        Step(cpu, 2);
        AssertCpu(4, new(0x0200 | 0b0000_0000, 0, 0, 0, 0, 0x0104), cpu);
    }

    [Fact]
    public void SubtractFromA()
    {
        var bus = Bus.From([
            0b1001_0000,        // sub a, b
            0b1001_0001,        // sub a, c
            0b1001_0010,        // sub a, d
            0b1001_0011,        // sub a, e
            0b1001_0100,        // sub a, h
            0b1001_0101,        // sub a, l
            0b1001_0110,        // sub a, [hl]
            0b1001_0111,        // sub a, a
        ]);
        bus.Write(0, 0x05);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0x0001, 0x020F, 0x0000, 0, 0x0100));

        cpu.Step();
        AssertCpu(1, new(0x0000 | 0b1100_0000, 0x0001, 0x020F, 0x0000, 0, 0x0101), cpu);
        cpu.Step();
        AssertCpu(2, new(0xFF00 | 0b0111_0000, 0x0001, 0x020F, 0x0000, 0, 0x0102), cpu);
        cpu.Step();
        AssertCpu(3, new(0xFD00 | 0b0100_0000, 0x0001, 0x020F, 0x0000, 0, 0x0103), cpu);
        cpu.Step();
        AssertCpu(4, new(0xEE00 | 0b0110_0000, 0x0001, 0x020F, 0x0000, 0, 0x0104), cpu);
        cpu.Step();
        AssertCpu(5, new(0xEE00 | 0b0100_0000, 0x0001, 0x020F, 0x0000, 0, 0x0105), cpu);
        cpu.Step();
        AssertCpu(6, new(0xEE00 | 0b0100_0000, 0x0001, 0x020F, 0x0000, 0, 0x0106), cpu);
        cpu.Step();
        AssertCpu(8, new(0xE900 | 0b0100_0000, 0x0001, 0x020F, 0x0000, 0, 0x0107), cpu);
        cpu.Step();
        AssertCpu(9, new(0x0000 | 0b1100_0000, 0x0001, 0x020F, 0x0000, 0, 0x0108), cpu);
    }

    [Fact]
    public void SubtractLiteral8FromA()
    {
        var bus = Bus.From([
            0b1101_0110, 0x0F   // sub a, 0x0F
        ]);
        var cpu = Cpu.Create(bus);

        cpu.Step();
        AssertCpu(2, new(0xF100 | 0b0111_0000, 0, 0, 0, 0, 0x0102), cpu);
    }

    [Fact]
    public void SubtractFromACarry()
    {
        var bus = Bus.From([
            0b1001_1000,        // sbc a, b
            0b1001_1001         // sbc a, c
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0x0101, 0, 0, 0, 0x0100));

        Step(cpu, 2);
        AssertCpu(2, new(0xFD00 | 0b0100_0000, 0x0101, 0, 0, 0, 0x0102), cpu);
    }

    [Fact]
    public void SubtractLiteral8FromACarry()
    {
        var bus = Bus.From([
            0b1101_1110, 0x01,  // sbc a, 0x01
            0b1101_1110, 0x01,  // sbc a, 0x01
        ]);
        var cpu = Cpu.Create(bus);

        Step(cpu, 2);
        AssertCpu(4, new(0xFD00 | 0b0100_0000, 0, 0, 0, 0, 0x0104), cpu);
    }

    [Fact]
    public void CompareToA()
    {
        var bus = Bus.From([
            0b1011_1000,        // cp a, b
            0b1011_1001,        // cp a, c
            0b1011_1010,        // cp a, d
            0b1011_1011,        // cp a, e
            0b1011_1100,        // cp a, h
            0b1011_1101,        // cp a, l
            0b1011_1110,        // cp a, [hl]
            0b1011_1111,        // cp a, a
        ]);
        bus.Write(0, 0x05);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0x1000, 0x1001, 0x2000, 0x0000, 0, 0x0100));

        cpu.Step();
        AssertCpu(1, new(0x1000 | 0b1100_0000, 0x1001, 0x2000, 0x0000, 0, 0x0101), cpu);
        cpu.Step();
        AssertCpu(2, new(0x1000 | 0b0110_0000, 0x1001, 0x2000, 0x0000, 0, 0x0102), cpu);
        cpu.Step();
        AssertCpu(3, new(0x1000 | 0b0101_0000, 0x1001, 0x2000, 0x0000, 0, 0x0103), cpu);
        cpu.Step();
        AssertCpu(4, new(0x1000 | 0b0100_0000, 0x1001, 0x2000, 0x0000, 0, 0x0104), cpu);
        cpu.Step();
        AssertCpu(5, new(0x1000 | 0b0100_0000, 0x1001, 0x2000, 0x0000, 0, 0x0105), cpu);
        cpu.Step();
        AssertCpu(6, new(0x1000 | 0b0100_0000, 0x1001, 0x2000, 0x0000, 0, 0x0106), cpu);
        cpu.Step();
        AssertCpu(8, new(0x1000 | 0b0110_0000, 0x1001, 0x2000, 0x0000, 0, 0x0107), cpu);
        cpu.Step();
        AssertCpu(9, new(0x1000 | 0b1100_0000, 0x1001, 0x2000, 0x0000, 0, 0x0108), cpu);
    }

    [Fact]
    public void CompareLiteral8ToA()
    {
        var bus = Bus.From([
            0b1111_1110, 0x0F   // cp a, 0x0F
        ]);
        var cpu = Cpu.Create(bus);

        cpu.Step();
        AssertCpu(2, new(0x0000 | 0b0111_0000, 0, 0, 0, 0, 0x0102), cpu);
    }

    // 16 Bit arithmetic
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
            0b0000_1001,                // add hl, bc
            0b0001_1001,                // add hl, de
            0b0010_1001,                // add hl, hl
            0b0011_1001,                // add hl, sp
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0x0FFF, 0x0001, 0, 0xE001, 0x0100));

        cpu.Step();
        AssertCpu(2, new(0b0000_0000, 0x0FFF, 0x0001, 0x0FFF, 0xE001, 0x0101), cpu);
        cpu.Step();
        AssertCpu(4, new(0b0010_0000, 0x0FFF, 0x0001, 0x1000, 0xE001, 0x0102), cpu);
        cpu.Step();
        AssertCpu(6, new(0b0000_0000, 0x0FFF, 0x0001, 0x2000, 0xE001, 0x0103), cpu);
        cpu.Step();
        AssertCpu(8, new(0b0001_0000, 0x0FFF, 0x0001, 0x0001, 0xE001, 0x0104), cpu);
    }

    // Bitwise logic
    [Fact]
    public void ComplementA()
    {
        var bus = Bus.From([
            0b0010_1111         // cpl
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0b1010_1010_0000_0000, 0, 0, 0, 0, 0x0100));

        cpu.Step();

        AssertCpu(1, new(0b0101_0101_0110_0000, 0, 0, 0, 0, 0x0101), cpu);
    }

    [Fact]
    public void AndWithA()
    {
        var bus = Bus.From([
            0b1010_0000,        // and a, b
            0b1010_0001,        // and a, c
            0b1010_0010,        // and a, d
            0b1010_0011,        // and a, e
            0b1010_0100,        // and a, h
            0b1010_0101,        // and a, l
            0b1010_0110,        // and a, [hl]
            0b1010_0111,        // and a, a
            0x00                // [hl]
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0xFF00, 0x0001, 0x02F3, 0x0108, 0, 0x0100));

        Step(cpu, 8);
        AssertCpu(9, new(0x0000 | 0b1010_0000, 0x0001, 0x02F3, 0x0108, 0, 0x0108), cpu);
    }

    [Fact]
    public void AndLiteral8WithA()
    {
        var bus = Bus.From([
            0b1110_0110, 0xF0,   // and a, 0xF0
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0x0F00, 0, 0, 0, 0, 0x0100));

        cpu.Step();
        AssertCpu(2, new(0x0000 | 0b1010_0000, 0, 0, 0, 0, 0x0102), cpu);
    }

    [Fact]
    public void XorWithA()
    {
        var bus = Bus.From([
            0b1010_1000,        // xor a, b
            0b1010_1001,        // xor a, c
            0b1010_1010,        // xor a, d
            0b1010_1011,        // xor a, e
            0b1010_1100,        // xor a, h
            0b1010_1101,        // xor a, l
            0b1010_1110,        // xor a, [hl]
            0b1010_1111,        // xor a, a
            0x00                // [hl]
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0xFF00, 0x0001, 0x02F3, 0x0108, 0, 0x0100));

        Step(cpu, 8);

        AssertCpu(9, new(0x0000 | 0b1000_0000, 0x0001, 0x02F3, 0x0108, 0, 0x0108), cpu);
    }

    [Fact]
    public void XorLiteral8WithA()
    {
        var bus = Bus.From([
            0b1110_1110, 0xF0,   // xor a, 0xF0
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0xF000, 0, 0, 0, 0, 0x0100));

        cpu.Step();
        AssertCpu(2, new(0x0000 | 0b1000_0000, 0, 0, 0, 0, 0x0102), cpu);
    }

    [Fact]
    public void OrWithA()
    {
        var bus = Bus.From([
            0b1011_0000,        // or a, b
            0b1011_0001,        // or a, c
            0b1011_0010,        // or a, d
            0b1011_0011,        // or a, e
            0b1011_0100,        // or a, h
            0b1011_0101,        // or a, l
            0b1011_0110,        // or a, [hl]
            0b1011_0111,        // or a, a
            0x04                // [hl]
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0x0000, 0x0001, 0x02F3, 0x0108, 0, 0x0100));

        cpu.Step();
        AssertCpu(1, new(0x0000 | 0b1000_0000, 0x0001, 0x02F3, 0x0108, 0, 0x0101), cpu);
        cpu.Step();
        AssertCpu(2, new(0x0100 | 0b0000_0000, 0x0001, 0x02F3, 0x0108, 0, 0x0102), cpu);
        cpu.Step();
        AssertCpu(3, new(0x0300 | 0b0000_0000, 0x0001, 0x02F3, 0x0108, 0, 0x0103), cpu);
        cpu.Step();
        AssertCpu(4, new(0xF300 | 0b0000_0000, 0x0001, 0x02F3, 0x0108, 0, 0x0104), cpu);
        cpu.Step();
        AssertCpu(5, new(0xF300 | 0b0000_0000, 0x0001, 0x02F3, 0x0108, 0, 0x0105), cpu);
        cpu.Step();
        AssertCpu(6, new(0xFB00 | 0b0000_0000, 0x0001, 0x02F3, 0x0108, 0, 0x0106), cpu);
        cpu.Step();
        AssertCpu(8, new(0xFF00 | 0b0000_0000, 0x0001, 0x02F3, 0x0108, 0, 0x0107), cpu);
        cpu.Step();
        AssertCpu(9, new(0xFF00 | 0b0000_0000, 0x0001, 0x02F3, 0x0108, 0, 0x0108), cpu);
    }

    [Fact]
    public void OrLiteral8WithA()
    {
        var bus = Bus.From([
            0b1111_0110, 0x00,   // or a, 0x00
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0x0000, 0, 0, 0, 0, 0x0100));

        cpu.Step();
        AssertCpu(2, new(0x0000 | 0b1000_0000, 0, 0, 0, 0, 0x0102), cpu);
    }

    // Bit shift
    [Fact]
    public void RotateLeftA_WithAndWithoutCarry()
    {
        var bus = Bus.From([
            0b0000_0111,                // rlca
            0b0001_0111                 // rla
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0b1000_1110_0000_0000, 0, 0, 0, 0, 0x0100));

        cpu.Step();
        AssertCpu(1, new(0b0001_1101_0001_0000, 0, 0, 0, 0, 0x0101), cpu);
        cpu.Step();
        AssertCpu(2, new(0b0011_1011_0000_0000, 0, 0, 0, 0, 0x0102), cpu);
    }

    [Fact]
    public void RotateRightA_WithAndWithoutCarry()
    {
        var bus = Bus.From([
            0b0000_1111,                // rrca
            0b0001_1111                 // rra
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0b0111_0001_0000_0000, 0, 0, 0, 0, 0x0100));

        cpu.Step();
        AssertCpu(1, new(0b1011_1000_0001_0000, 0, 0, 0, 0, 0x0101), cpu);
        cpu.Step();
        AssertCpu(2, new(0b1101_1100_0000_0000, 0, 0, 0, 0, 0x0102), cpu);
    }

    // Jump and subroutine
    [Fact]
    public void JumpRelative()
    {
        var bus = Bus.From([
            0b0001_1000, 0x01,  // jr 1
            0x00,               // nop
            0b0001_1000, 0xFF   // jr -1
        ]);
        var cpu = Cpu.Create(bus);

        Step(cpu, 2);

        AssertCpu(6, new(0, 0, 0, 0, 0, 0x0104), cpu);
    }

    [Theory]
    [InlineData(0b1000_0000, 0b0010_0000, 1, 2, 0x0102)]        // jr nz, 1
    [InlineData(0b0000_0000, 0b0010_0000, 1, 3, 0x0103)]        // jr nz, 1
    [InlineData(0b0000_0000, 0b0010_0000, 0xFF, 3, 0x0101)]     // jr nz, -1
    [InlineData(0b0000_0000, 0b0010_1000, 1, 2, 0x0102)]        // jr z, 1
    [InlineData(0b1000_0000, 0b0010_1000, 1, 3, 0x0103)]        // jr z, 1
    [InlineData(0b0001_0000, 0b0011_0000, 1, 2, 0x0102)]        // jr nc, 1
    [InlineData(0b0000_0000, 0b0011_0000, 1, 3, 0x0103)]        // jr nc, 1
    [InlineData(0b0000_0000, 0b0011_1000, 1, 2, 0x0102)]        // jr c, 1
    [InlineData(0b0001_0000, 0b0011_1000, 1, 3, 0x0103)]        // jr c, 1
    public void ConditionalJumpRelative(byte flags, byte opcode, byte address, ulong expectedTicks, ushort expectedProgramCounter)
    {
        var bus = Bus.From([
            opcode, address
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(flags, 0, 0, 0, 0, 0x0100));

        cpu.Step();

        AssertCpu(expectedTicks, new(flags, 0, 0, 0, 0, expectedProgramCounter), cpu);
    }

    [Fact]
    public void Jump()
    {
        var bus = Bus.From([
            0b1100_0011, 0x01, 0x02   // jp 0
        ]);
        var cpu = Cpu.Create(bus);

        cpu.Step();

        AssertCpu(4, new(0, 0, 0, 0, 0, 0x0201), cpu);
    }

    [Fact]
    public void ConditionalJump()
    {
        var bus = Bus.From([
            0b1100_0010, 0x04, 0x01,    // jp nz, 0x0104
            0,                          // nop
            0b1100_1010, 0x00, 0x00,    // jp z, 0
            0,                          // nop
            0b1101_0010, 0x0C, 0x01,    // jp nc, 0x010C
            0,                          // nop
            0b1101_1010, 0x00, 0x00     // jp c, 0
        ]);
        var cpu = Cpu.Create(bus);

        Step(cpu, 5);

        AssertCpu(15, new(0, 0, 0, 0, 0, 0x010F), cpu);
    }

    [Fact]
    public void JumpHL()
    {
        var bus = Bus.From([
            0b1110_1001,    // jp hl
        ]);
        var cpu = Cpu.Create(bus);

        cpu.Step();

        AssertCpu(1, new(0, 0, 0, 0, 0, 0x0), cpu);
    }

    [Fact]
    public void Call()
    {
        var bus = Bus.From([
            0b1100_1101, 0x02, 0x01     // call 0x0102
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0, 0, 0, 0x0005, 0x0100));

        cpu.Step();

        AssertCpu(6, new(0, 0, 0, 0, 0x0003, 0x0102), cpu);
        Assert.Equal(0x01, bus.Read(0x04));
        Assert.Equal(0x03, bus.Read(0x03));
    }

    [Fact]
    public void ConditionalCall()
    {
        var bus = Bus.From([
            0b1100_0100, 0x04, 0x01,    // call nz, 0x0104
            0,                          // nop
            0b1100_1100, 0x08, 0x01,    // call z, 0x0108
            0,                          // nop
            0b1101_0100, 0x0C, 0x01,    // call nc, 0x010C
            0,                          // nop
            0b1101_1100, 0x00, 0x00     // call c, 0
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0, 0, 0, 0x0004, 0x0100));

        Step(cpu, 5);

        AssertCpu(19, new(0, 0, 0, 0, 0x0000, 0x010F), cpu);
        Assert.Equal(0x01, bus.Read(0x03));
        Assert.Equal(0x03, bus.Read(0x02));
        Assert.Equal(0x01, bus.Read(0x01));
        Assert.Equal(0x0B, bus.Read(0x00));
    }

    [Fact]
    public void Restart()
    {
        var bus = Bus.From([
            0b1111_1111                 // rst 7
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0, 0, 0, 0x0005, 0x0100));

        cpu.Step();

        AssertCpu(4, new(0, 0, 0, 0, 0x0003, 7 * 8), cpu);
        Assert.Equal(0x01, bus.Read(0x04));
        Assert.Equal(0x01, bus.Read(0x03));
    }

    [Fact]
    public void Return()
    {
        var bus = Bus.From([
            0b1100_1001         // ret
        ]);
        bus.Write(0, 0x02);
        bus.Write(1, 0x01);
        var cpu = Cpu.Create(bus);

        cpu.Step();

        AssertCpu(4, new(0, 0, 0, 0, 2, 0x0102), cpu);
    }

    [Fact]
    public void ConditionalReturn()
    {
        var bus = Bus.From([
            0b1100_0000,        // ret nz
            0,                  // nop
            0b1100_1000,        // ret z
            0,                  // nop
            0b1101_0000,        // ret nc
            0,                  // nop
            0b1101_1000         // ret c
        ]);
        bus.Write(0, 0x02);
        bus.Write(1, 0x01);
        bus.Write(2, 0x06);
        bus.Write(3, 0x01);
        var cpu = Cpu.Create(bus);

        Step(cpu, 5);

        AssertCpu(15, new(0, 0, 0, 0, 0x0004, 0x0107), cpu);
    }

    [Fact]
    public void ReturnInterrupt()
    {
        var bus = Bus.From([
            0b1101_1001         // reti
        ]);
        bus.Write(0, 0x02);
        bus.Write(1, 0x01);
        var cpu = Cpu.Create(bus);

        cpu.Step();

        AssertCpu(4, new(0, 0, 0, 0, 2, 0x0102, Interrupt: true), cpu);
    }

    // Carry flag
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

    // Stack manipulation
    [Fact]
    public void Push()
    {
        var bus = Bus.From([
            0b1100_0101,        // push bc
            0b1101_0101,        // push de
            0b1110_0101,        // push hl
            0b1111_0101,        // push af
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0x0708, 0x0102, 0x0304, 0x0506, 0x0008, 0x0100));

        Step(cpu, 4);

        AssertCpu(16, new(0x0708, 0x0102, 0x0304, 0x0506, 0x0000, 0x0104), cpu);
        Assert.Equal(1, bus.Read(0x0007));
        Assert.Equal(2, bus.Read(0x0006));
        Assert.Equal(3, bus.Read(0x0005));
        Assert.Equal(4, bus.Read(0x0004));
        Assert.Equal(5, bus.Read(0x0003));
        Assert.Equal(6, bus.Read(0x0002));
        Assert.Equal(7, bus.Read(0x0001));
        Assert.Equal(8, bus.Read(0x0000));
    }

    [Fact]
    public void Pop()
    {
        var bus = Bus.From([
            0b1100_0001,        // pop bc
            0b1101_0001,        // pop de
            0b1110_0001,        // pop hl
            0b1111_0001,        // pop af
        ]);
        bus.Write(0, 2);
        bus.Write(1, 1);
        bus.Write(2, 4);
        bus.Write(3, 3);
        bus.Write(4, 6);
        bus.Write(5, 5);
        bus.Write(6, 8);
        bus.Write(7, 7);
        var cpu = Cpu.Create(bus);

        Step(cpu, 4);

        AssertCpu(12, new(0x0708, 0x0102, 0x0304, 0x0506, 0x0008, 0x0104), cpu);
    }

    [Fact]
    public void AddSignedLiteral8ToStackPointer()
    {
        var bus = Bus.From([
            0b1110_1000, 0x7F,  // add sp, 0x7F
            0b1110_1000, 0x7F,  // add sp, 0x7F
            0b1110_1000, 0x7F,  // add sp, 0x7F
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0, 0, 0, 0x000F, 0x0100));

        cpu.Step();
        AssertCpu(4, new(0b0010_0000, 0, 0, 0, 0x008E, 0x0102), cpu);
        cpu.Step();
        AssertCpu(8, new(0b0011_0000, 0, 0, 0, 0x010D, 0x0104), cpu);
        cpu.Step();
        AssertCpu(12, new(0b0010_0000, 0, 0, 0, 0x018C, 0x0106), cpu);
    }

    [Fact]
    public void LoadFromStackPointerIntoLiteral16Pointer()
    {
        var bus = Bus.From([
            0b0000_1000, 0x01, 0x00     // ld [0x0001], sp
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0, 0, 0, 0x0807, 0x0100));

        cpu.Step();

        AssertCpu(5, new(0, 0, 0, 0, 0x0807, 0x0103), cpu);
        Assert.Equal(0x07, bus.Read(1));
        Assert.Equal(0x08, bus.Read(2));
    }

    [Theory]
    [InlineData(0b0010_0000, 0x000F, 0x008E)]
    [InlineData(0b0001_0000, 0x00F0, 0x016F)]
    [InlineData(0b0011_0000, 0x00FF, 0x017E)]
    public void LoadFromStackPointerPlusSignedLiteral8IntoHL(byte flags, ushort stackPointer, ushort hl)
    {
        var bus = Bus.From([
            0b1111_1000, 0x7F,  // ld hl, sp + 0x7F
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0, 0, 0, stackPointer, 0x0100));

        cpu.Step();

        AssertCpu(3, new(flags, 0, 0, hl, stackPointer, 0x0102), cpu);
    }

    [Fact]
    public void LoadFromHLIntoStackPointer()
    {
        var bus = Bus.From([0b1111_1001]);  // ld sp, hl
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0, 0, 0xFFFF, 0, 0x0100));

        cpu.Step();

        AssertCpu(2, new(0, 0, 0, 0xFFFF, 0xFFFF, 0x0101), cpu);
    }

    // 16 Bit instructions
    // Bit shift
    [Fact]
    public void PrefixedRotateLeft()
    {
        var bus = Bus.From([
            0xCB, 0b0000_0000,  // rlc b
            0xCB, 0b0000_0001,  // rlc c
            0xCB, 0b0000_0010,  // rlc d
            0xCB, 0b0000_0011,  // rlc e
            0xCB, 0b0000_0100,  // rlc h
            0xCB, 0b0000_0101,  // rlc l
            0xCB, 0b0000_0110,  // rlc [hl]
            0xCB, 0b0000_0111,  // rlc a
        ]);
        bus.Write(0, 0b1010_1010);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0xAA00, 0xAAAA, 0xAAAA, 0, 0, 0x0100));

        cpu.Step();
        AssertCpu(2, new(0xAA00 | 0b0001_0000, 0x55AA, 0xAAAA, 0, 0, 0x0102), cpu);
        cpu.Step();
        AssertCpu(4, new(0xAA00 | 0b0001_0000, 0x5555, 0xAAAA, 0, 0, 0x0104), cpu);
        cpu.Step();
        AssertCpu(6, new(0xAA00 | 0b0001_0000, 0x5555, 0x55AA, 0, 0, 0x0106), cpu);
        cpu.Step();
        AssertCpu(8, new(0xAA00 | 0b0001_0000, 0x5555, 0x5555, 0, 0, 0x0108), cpu);
        cpu.Step();
        AssertCpu(10, new(0xAA00 | 0b1000_0000, 0x5555, 0x5555, 0, 0, 0x010A), cpu);
        cpu.Step();
        AssertCpu(12, new(0xAA00 | 0b1000_0000, 0x5555, 0x5555, 0, 0, 0x010C), cpu);
        cpu.Step();
        AssertCpu(16, new(0xAA00 | 0b0001_0000, 0x5555, 0x5555, 0, 0, 0x010E), cpu);
        Assert.Equal(0x55, bus.Read(0));
        cpu.Step();
        AssertCpu(18, new(0x5500 | 0b0001_0000, 0x5555, 0x5555, 0, 0, 0x0110), cpu);
    }

    [Fact]
    public void PrefixedRotateLeftThroughCarry()
    {
        var bus = Bus.From([
            0xCB, 0b0001_0000,  // rl b
            0xCB, 0b0001_0001,  // rl c
            0xCB, 0b0001_0010,  // rl d
            0xCB, 0b0001_0011,  // rl e
            0xCB, 0b0001_0110,  // rl [hl]
            0xCB, 0b0001_0100,  // rl h
            0xCB, 0b0001_0101,  // rl l
            0xCB, 0b0001_0111,  // rl a
        ]);
        bus.Write(0, 0b1010_1010);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0xAA00, 0xAAAA, 0xAAAA, 0, 0, 0x0100));

        cpu.Step();
        AssertCpu(2, new(0xAA00 | 0b0001_0000, 0x54AA, 0xAAAA, 0, 0, 0x0102), cpu);
        cpu.Step();
        AssertCpu(4, new(0xAA00 | 0b0001_0000, 0x5455, 0xAAAA, 0, 0, 0x0104), cpu);
        cpu.Step();
        AssertCpu(6, new(0xAA00 | 0b0001_0000, 0x5455, 0x55AA, 0, 0, 0x0106), cpu);
        cpu.Step();
        AssertCpu(8, new(0xAA00 | 0b0001_0000, 0x5455, 0x5555, 0, 0, 0x0108), cpu);
        cpu.Step();
        AssertCpu(12, new(0xAA00 | 0b0001_0000, 0x5455, 0x5555, 0, 0, 0x010A), cpu);
        Assert.Equal(0x55, bus.Read(0));
        cpu.Step();
        AssertCpu(14, new(0xAA00 | 0b0000_0000, 0x5455, 0x5555, 0x0100, 0, 0x010C), cpu);
        cpu.Step();
        AssertCpu(16, new(0xAA00 | 0b1000_0000, 0x5455, 0x5555, 0x0100, 0, 0x010E), cpu);
        cpu.Step();
        AssertCpu(18, new(0x5400 | 0b0001_0000, 0x5455, 0x5555, 0x0100, 0, 0x0110), cpu);
    }

    [Fact]
    public void PrefixedRotateRight()
    {
        var bus = Bus.From([
            0xCB, 0b0000_1000,  // rrc b
            0xCB, 0b0000_1001,  // rrc c
            0xCB, 0b0000_1010,  // rrc d
            0xCB, 0b0000_1011,  // rrc e
            0xCB, 0b0000_1100,  // rrc h
            0xCB, 0b0000_1101,  // rrc l
            0xCB, 0b0000_1110,  // rrc [hl]
            0xCB, 0b0000_1111,  // rrc a
        ]);
        bus.Write(0, 0x55);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0x5500, 0x5555, 0x5555, 0, 0, 0x0100));

        cpu.Step();
        AssertCpu(2, new(0x5500 | 0b0001_0000, 0xAA55, 0x5555, 0, 0, 0x0102), cpu);
        cpu.Step();
        AssertCpu(4, new(0x5500 | 0b0001_0000, 0xAAAA, 0x5555, 0, 0, 0x0104), cpu);
        cpu.Step();
        AssertCpu(6, new(0x5500 | 0b0001_0000, 0xAAAA, 0xAA55, 0, 0, 0x0106), cpu);
        cpu.Step();
        AssertCpu(8, new(0x5500 | 0b0001_0000, 0xAAAA, 0xAAAA, 0, 0, 0x0108), cpu);
        cpu.Step();
        AssertCpu(10, new(0x5500 | 0b1000_0000, 0xAAAA, 0xAAAA, 0, 0, 0x010A), cpu);
        cpu.Step();
        AssertCpu(12, new(0x5500 | 0b1000_0000, 0xAAAA, 0xAAAA, 0, 0, 0x010C), cpu);
        cpu.Step();
        AssertCpu(16, new(0x5500 | 0b0001_0000, 0xAAAA, 0xAAAA, 0, 0, 0x010E), cpu);
        Assert.Equal(0xAA, bus.Read(0));
        cpu.Step();
        AssertCpu(18, new(0xAA00 | 0b0001_0000, 0xAAAA, 0xAAAA, 0, 0, 0x0110), cpu);
    }

    [Fact]
    public void PrefixedRotateRightThroughCarry()
    {
        var bus = Bus.From([
            0xCB, 0b0001_1000,  // rr b
            0xCB, 0b0001_1001,  // rr c
            0xCB, 0b0001_1010,  // rr d
            0xCB, 0b0001_1011,  // rr e
            0xCB, 0b0001_1110,  // rr [hl]
            0xCB, 0b0001_1100,  // rr h
            0xCB, 0b0001_1101,  // rr l
            0xCB, 0b0001_1111,  // rr a
        ]);
        bus.Write(0, 0x55);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0x5500, 0x5555, 0x5555, 0, 0, 0x0100));

        cpu.Step();
        AssertCpu(2, new(0x5500 | 0b0001_0000, 0x2A55, 0x5555, 0, 0, 0x0102), cpu);
        cpu.Step();
        AssertCpu(4, new(0x5500 | 0b0001_0000, 0x2AAA, 0x5555, 0, 0, 0x0104), cpu);
        cpu.Step();
        AssertCpu(6, new(0x5500 | 0b0001_0000, 0x2AAA, 0xAA55, 0, 0, 0x0106), cpu);
        cpu.Step();
        AssertCpu(8, new(0x5500 | 0b0001_0000, 0x2AAA, 0xAAAA, 0, 0, 0x0108), cpu);
        cpu.Step();
        AssertCpu(12, new(0x5500 | 0b0001_0000, 0x2AAA, 0xAAAA, 0, 0, 0x010A), cpu);
        Assert.Equal(0xAA, bus.Read(0));
        cpu.Step();
        AssertCpu(14, new(0x5500 | 0b0000_0000, 0x2AAA, 0xAAAA, 0x8000, 0, 0x010C), cpu);
        cpu.Step();
        AssertCpu(16, new(0x5500 | 0b1000_0000, 0x2AAA, 0xAAAA, 0x8000, 0, 0x010E), cpu);
        cpu.Step();
        AssertCpu(18, new(0x2A00 | 0b0001_0000, 0x2AAA, 0xAAAA, 0x8000, 0, 0x0110), cpu);
    }

    [Fact]
    public void PrefixedShiftLeftArithmetic()
    {
        var bus = Bus.From([
            0xCB, 0b0010_0000,  // sla b
            0xCB, 0b0010_0001,  // sla c
            0xCB, 0b0010_0010,  // sla d
            0xCB, 0b0010_0011,  // sla e
            0xCB, 0b0010_0100,  // sla h
            0xCB, 0b0010_0101,  // sla l
            0xCB, 0b0010_0110,  // sla [hl]
            0xCB, 0b0010_0111,  // sla a
        ]);
        bus.Write(0, 0xAA);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0xAA00, 0xAAAA, 0xAAAA, 0, 0, 0x0100));

        Step(cpu, 4);
        AssertCpu(8, new(0xAA00 | 0b0001_0000, 0x5454, 0x5454, 0, 0, 0x0108), cpu);
        Step(cpu, 2);
        AssertCpu(12, new(0xAA00 | 0b1000_0000, 0x5454, 0x5454, 0, 0, 0x010C), cpu);
        Step(cpu, 2);
        AssertCpu(18, new(0x5400 | 0b0001_0000, 0x5454, 0x5454, 0, 0, 0x0110), cpu);
        Assert.Equal(0x54, bus.Read(0));
    }

    [Fact]
    public void PrefixedShiftRightArithmetic()
    {
        var bus = Bus.From([
            0xCB, 0b0010_1000,  // sra b
            0xCB, 0b0010_1001,  // sra c
            0xCB, 0b0010_1010,  // sra d
            0xCB, 0b0010_1011,  // sra e
            0xCB, 0b0010_1100,  // sra h
            0xCB, 0b0010_1101,  // sra l
            0xCB, 0b0010_1110,  // sra [hl]
            0xCB, 0b0010_1111,  // sra a
        ]);
        bus.Write(0, 0xA5);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0xA500, 0xA5A5, 0xA5A5, 0, 0, 0x0100));

        Step(cpu, 4);
        AssertCpu(8, new(0xA500 | 0b0001_0000, 0xD2D2, 0xD2D2, 0, 0, 0x0108), cpu);
        Step(cpu, 2);
        AssertCpu(12, new(0xA500 | 0b1000_0000, 0xD2D2, 0xD2D2, 0, 0, 0x010C), cpu);
        Step(cpu, 2);
        AssertCpu(18, new(0xD200 | 0b0001_0000, 0xD2D2, 0xD2D2, 0, 0, 0x0110), cpu);
        Assert.Equal(0xD2, bus.Read(0));
    }

    [Fact]
    public void PrefixedSwap()
    {
        var bus = Bus.From([
            0xCB, 0b0011_0000,  // swap b
            0xCB, 0b0011_0001,  // swap c
            0xCB, 0b0011_0010,  // swap d
            0xCB, 0b0011_0011,  // swap e
            0xCB, 0b0011_0100,  // swap h
            0xCB, 0b0011_0101,  // swap l
            0xCB, 0b0011_0110,  // swap [hl]
            0xCB, 0b0011_0111,  // swap a
        ]);
        bus.Write(0, 0x1E);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0x1E00, 0x1E1E, 0x1E1E, 0, 0, 0x0100));

        Step(cpu, 5);
        AssertCpu(10, new(0x1E00 | 0b1000_0000, 0xE1E1, 0xE1E1, 0, 0, 0x010A), cpu);
        Step(cpu, 3);
        AssertCpu(18, new(0xE100 | 0b0000_0000, 0xE1E1, 0xE1E1, 0, 0, 0x0110), cpu);
        Assert.Equal(0xE1, bus.Read(0));
    }

    [Fact]
    public void PrefixedShiftRightLogical()
    {
        var bus = Bus.From([
            0xCB, 0b0011_1000,  // srl b
            0xCB, 0b0011_1001,  // srl c
            0xCB, 0b0011_1010,  // srl d
            0xCB, 0b0011_1011,  // srl e
            0xCB, 0b0011_1100,  // srl h
            0xCB, 0b0011_1101,  // srl l
            0xCB, 0b0011_1110,  // srl [hl]
            0xCB, 0b0011_1111,  // srl a
        ]);
        bus.Write(0, 0xA5);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0xA500, 0xA5A5, 0xA5A5, 0, 0, 0x0100));

        Step(cpu, 4);
        AssertCpu(8, new(0xA500 | 0b0001_0000, 0x5252, 0x5252, 0, 0, 0x0108), cpu);
        Step(cpu, 2);
        AssertCpu(12, new(0xA500 | 0b1000_0000, 0x5252, 0x5252, 0, 0, 0x010C), cpu);
        Step(cpu, 2);
        AssertCpu(18, new(0x5200 | 0b0001_0000, 0x5252, 0x5252, 0, 0, 0x0110), cpu);
        Assert.Equal(0x52, bus.Read(0));
    }

    // 16 Bit instructions
    // Bit flag
    [Fact]
    public void PrefixedCheckBit()
    {
        var bus = Bus.From([
            0xCB, 0b0100_0000,  // bit 0, b
            0xCB, 0b0100_1001,  // bit 1, c
            0xCB, 0b0101_0010,  // bit 2, d
            0xCB, 0b0101_1011,  // bit 3, e
            0xCB, 0b0110_0100,  // bit 4, h
            0xCB, 0b0110_1101,  // bit 5, l
            0xCB, 0b0111_0110,  // bit 6, [hl]
            0xCB, 0b0111_1111,  // bit 7, a
        ]);
        bus.Write(0, 0x40);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0x8000, 0x0102, 0x0408, 0, 0, 0x0100));

        cpu.Step();
        AssertCpu(2, new(0x8000 | 0b1010_0000, 0x0102, 0x0408, 0, 0, 0x0102), cpu);
        cpu.Step();
        AssertCpu(4, new(0x8000 | 0b1010_0000, 0x0102, 0x0408, 0, 0, 0x0104), cpu);
        cpu.Step();
        AssertCpu(6, new(0x8000 | 0b1010_0000, 0x0102, 0x0408, 0, 0, 0x0106), cpu);
        cpu.Step();
        AssertCpu(8, new(0x8000 | 0b1010_0000, 0x0102, 0x0408, 0, 0, 0x0108), cpu);
        cpu.Step();
        AssertCpu(10, new(0x8000 | 0b0010_0000, 0x0102, 0x0408, 0, 0, 0x010A), cpu);
        cpu.Step();
        AssertCpu(12, new(0x8000 | 0b0010_0000, 0x0102, 0x0408, 0, 0, 0x010C), cpu);
        cpu.Step();
        AssertCpu(15, new(0x8000 | 0b1010_0000, 0x0102, 0x0408, 0, 0, 0x010E), cpu);
        cpu.Step();
        AssertCpu(17, new(0x8000 | 0b1010_0000, 0x0102, 0x0408, 0, 0, 0x0110), cpu);
    }

    [Fact]
    public void PrefixedSetBit()
    {
        var registers = new int[] { 6, 0, 1, 2, 3, 4, 5, 7 };
        var program = registers
            .SelectMany(r => Enumerable.Range(0, 8).Select(b => (r, b)))
            .Select(x => (byte)(0b1100_0000 | x.r | (x.b << 3)))
            .SelectMany(x => new byte[] { 0xCB, x });
        var bus = Bus.From(program);
        var cpu = Cpu.Create(bus);

        Step(cpu, 64);

        AssertCpu(8 * 4 + 56 * 2, new(0xFF00, 0xFFFF, 0xFFFF, 0xFFFF, 0, 0x0100 + 2 * 64), cpu);
        Assert.Equal(0xFF, bus.Read(0));
    }

    [Fact]
    public void PrefixedResetBit()
    {
        var registers = new int[] { 0, 1, 2, 3, 4, 5, 6, 7 };
        var program = registers
            .SelectMany(r => Enumerable.Range(0, 8).Select(b => (r, b)))
            .Select(x => (byte)(0b1000_0000 | x.r | (x.b << 3)))
            .SelectMany(x => new byte[] { 0xCB, x });
        var bus = Bus.From(program);
        bus.Write(0, 0xFF);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0xFF00, 0xFFFF, 0xFFFF, 0xFFFF, 0, 0x0100));

        Step(cpu, 64);

        AssertCpu(8 * 4 + 56 * 2, new(0x0000, 0x0000, 0x0000, 0x0000, 0, 0x0100 + 2 * 64), cpu);
        Assert.Equal(0x00, bus.Read(0));
    }

    private static void Step(Cpu cpu, int amount)
    {
        foreach (var _ in Enumerable.Repeat(0, amount))
            cpu.Step();
    }

    private static void AssertCpu(ulong expectedTicks, Cpu.RegisterState expectedState, Cpu actual)
    {
        Assert.Equal(expectedTicks, actual.Ticks);
        Assert.Equal(expectedState, actual.GetRegisterState());
    }
}
