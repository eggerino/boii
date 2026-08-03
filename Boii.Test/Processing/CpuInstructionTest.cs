using Boii.Test.Mock;
using static Boii.Processing.Test.CpuTestUtil;

namespace Boii.Processing.Test;

public class CpuInstructionTest
{
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
}
