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
    public void LoadFromStackPointer()
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
            0b0000_1001,                // dec bc
            0b0001_1001,                // dec de
            0b0010_1001,                // dec hl
            0b0011_1001,                // dec sp
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0x0FFF, 0x0001, 0, 0xE001, 0x0100));

        cpu.Step();
        AssertCpu(2, new(0b0000_0000_0000_0000, 0x0FFF, 0x0001, 0x0FFF, 0xE001, 0x0101), cpu);
        cpu.Step();
        AssertCpu(4, new(0b0000_0000_0010_0000, 0x0FFF, 0x0001, 0x1000, 0xE001, 0x0102), cpu);
        cpu.Step();
        AssertCpu(6, new(0b0000_0000_0010_0000, 0x0FFF, 0x0001, 0x2000, 0xE001, 0x0103), cpu);
        cpu.Step();
        AssertCpu(8, new(0b0000_0000_0011_0000, 0x0FFF, 0x0001, 0x0001, 0xE001, 0x0104), cpu);
    }

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

        Step(cpu, 8);

        AssertCpu(10, new(0x0100 | 0b1010_0000, 0x0010, 0x0101, 0x0101, 0x0000, 0x0108), cpu);
        Assert.Equal(0x01, bus.Read(0x0000));
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
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0x0100, 0, 0, 0, 0x0100));

        Step(cpu, 8);

        AssertCpu(10, new(0xFF00 | 0b1110_0000, 0x00FF, 0xFFFF, 0xFFFF, 0x0000, 0x0108), cpu);
        Assert.Equal(0xFF, bus.Read(0x0000));
    }

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
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0x0001, 0x02F3, 0x0000, 0, 0x0100));

        Step(cpu, 8);
        AssertCpu(9, new(0x0C00 | 0b1011_0000, 0x0001, 0x02F3, 0x0000, 0, 0x0108), cpu);
    }

    [Fact]
    public void AddToAImm8()
    {
        var bus = Bus.From([
            0b1100_0110, 0x10,  // add a, 0x10
            0b1100_0110, 0xF0,  // add a, 0xF0
        ]);
        var cpu = Cpu.Create(bus);

        Step(cpu, 2);
        AssertCpu(4, new(0x0000 | 0b1011_0000, 0, 0, 0, 0, 0x0104), cpu);
    }

    [Fact]
    public void AddToACarry()
    {
        var bus = Bus.From([
            0b1000_1000,        // adc a, b
            0b1000_1001,        // adc a, c
            0b1000_1010,        // adc a, d
            0b1000_1011,        // adc a, e
            0b1000_1100,        // adc a, h
            0b1000_1101,        // adc a, l
            0b1000_1110,        // adc a, [hl]
            0b1000_1111,        // adc a, a
        ]);
        bus.Write(0, 0x10);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0x0001, 0x02F3, 0x0000, 0, 0x0100));

        Step(cpu, 8);
        AssertCpu(9, new(0x0D00 | 0b1011_0000, 0x0001, 0x02F3, 0x0000, 0, 0x0108), cpu);
    }

    [Fact]
    public void AddToAImm8Carry()
    {
        var bus = Bus.From([
            0b1100_1110, 0x10,  // adc a, 0x10
            0b1100_1110, 0xF0,  // adc a, 0xF0
            0b1100_1110, 0x00,  // adc a, 0x00
        ]);
        var cpu = Cpu.Create(bus);

        Step(cpu, 3);
        AssertCpu(6, new(0x0100 | 0b1011_0000, 0, 0, 0, 0, 0x0106), cpu);
    }

    [Fact]
    public void SubtractToA()
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
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0x0001, 0x02F3, 0x0000, 0, 0x0100));

        Step(cpu, 8);
        AssertCpu(9, new(0x0000 | 0b1111_0000, 0x0001, 0x02F3, 0x0000, 0, 0x0108), cpu);
    }

    [Fact]
    public void SubtractToAImm8()
    {
        var bus = Bus.From([
            0b1101_0110, 0x0F,  // sub a, 0x0F
            0b1101_0110, 0xF1,  // sub a, 0xF1
        ]);
        var cpu = Cpu.Create(bus);

        Step(cpu, 2);
        AssertCpu(4, new(0x0000 | 0b1111_0000, 0, 0, 0, 0, 0x0104), cpu);
    }

    [Fact]
    public void SubtractToACarry()
    {
        var bus = Bus.From([
            0b1001_1000,        // sbc a, b
            0b1001_1001,        // sbc a, c
            0b1001_1010,        // sbc a, d
            0b1001_1011,        // sbc a, e
            0b1001_1100,        // sbc a, h
            0b1001_1101,        // sbc a, l
            0b1001_1110,        // sbc a, [hl]
            0b1001_1111,        // sbc a, a
        ]);
        bus.Write(0, 0x05);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0x0001, 0x02F3, 0x0000, 0, 0x0100));

        Step(cpu, 8);
        AssertCpu(9, new(0xFF00 | 0b1111_0000, 0x0001, 0x02F3, 0x0000, 0, 0x0108), cpu);
    }

    [Fact]
    public void SubtractToAImm8Carry()
    {
        var bus = Bus.From([
            0b1101_1110, 0x0F,  // sbc a, 0x0F
            0b1101_1110, 0xF0,  // sbc a, 0xF1
        ]);
        var cpu = Cpu.Create(bus);

        Step(cpu, 2);
        AssertCpu(4, new(0x0000 | 0b1111_0000, 0, 0, 0, 0, 0x0104), cpu);
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
        var cpu = Cpu.CreateWithRegisterState(bus, new(0x1000, 0x0001, 0x02F3, 0x0000, 0, 0x0100));

        Step(cpu, 8);
        AssertCpu(9, new(0x1000 | 0b1111_0000, 0x0001, 0x02F3, 0x0000, 0, 0x0108), cpu);
    }

    [Fact]
    public void CompareToAImm8()
    {
        var bus = Bus.From([
            0b1111_1110, 0x0F,  // cp a, 0x0F
            0b1111_1110, 0x00,  // cp a, 0x00
        ]);
        var cpu = Cpu.Create(bus);

        Step(cpu, 2);
        AssertCpu(4, new(0x0000 | 0b1111_0000, 0, 0, 0, 0, 0x0104), cpu);
    }

    [Fact]
    public void AndToA()
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
    public void AndToAImm8()
    {
        var bus = Bus.From([
            0b1110_0110, 0xF0,   // and a, 0xF0
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0x0F00, 0, 0, 0, 0, 0x0100));

        cpu.Step();
        AssertCpu(2, new(0x0000 | 0b1010_0000, 0, 0, 0, 0, 0x0102), cpu);
    }

    [Fact]
    public void XorToA()
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
    public void XorToAImm8()
    {
        var bus = Bus.From([
            0b1110_1110, 0xF0,   // xor a, 0xF0
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0xF000, 0, 0, 0, 0, 0x0100));

        cpu.Step();
        AssertCpu(2, new(0x0000 | 0b1000_0000, 0, 0, 0, 0, 0x0102), cpu);
    }

    [Fact]
    public void OrToA()
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

        Step(cpu, 8);
        AssertCpu(9, new(0xFF00 | 0b1000_0000, 0x0001, 0x02F3, 0x0108, 0, 0x0108), cpu);
    }

    [Fact]
    public void OrToAImm8()
    {
        var bus = Bus.From([
            0b1111_0110, 0x00,   // or a, 0x00
        ]);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0x0000, 0, 0, 0, 0, 0x0100));

        cpu.Step();
        AssertCpu(2, new(0x0000 | 0b1000_0000, 0, 0, 0, 0, 0x0102), cpu);
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
