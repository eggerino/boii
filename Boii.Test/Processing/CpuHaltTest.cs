using Boii.Test.Mock;
using static Boii.Processing.Test.CpuTestUtil;

namespace Boii.Processing.Test;

public class CpuHaltTest
{
    [Fact]
    public void CpuHaltsUntilInterrupt()
    {
        var bus = Bus.From([0b0111_0110]);  // halt
        bus.EnsureSize(0x1_0000);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0, 0, 0, 2, 0x0100, InterruptMaster: true));

        Step(cpu, 2);
        AssertCpu(2, new(0, 0, 0, 0, 2, 0x0101, InterruptMaster: true, Halted: true), cpu);

        bus.Write(0xFF0F, 0b0001_1111);
        bus.Write(0xFFFF, 0b0001_1111);

        cpu.Step();
        AssertCpu(7, new(0, 0, 0, 0, 0, 0x0040, InterruptMaster: false, Halted: false), cpu);
        Assert.Equal(0x01, bus.Read(0));
        Assert.Equal(0x01, bus.Read(1));
    }

    [Fact]
    public void CpuWakesOnInterrupt()
    {
        var bus = Bus.From([0b0111_0110]);  // halt
        bus.EnsureSize(0x1_0000);
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0, 0, 0, 0, 0x0100));

        Step(cpu, 2);
        AssertCpu(2, new(0, 0, 0, 0, 0, 0x0101, Halted: true), cpu);

        bus.Write(0xFF0F, 0b0001_1111);
        bus.Write(0xFFFF, 0b0001_1111);

        cpu.Step();
        AssertCpu(3, new(0, 0, 0, 0, 0, 0x0102), cpu);
    }

    [Fact]
    public void HaltBug_RereadNextByte()
    {
        var bus = Bus.From([
            0b0111_0110,    // halt
            0x06, 0x04,     // ld b, 4
        ]);
        bus.EnsureSize(0x1_0000);
        bus.Write(0xFF0F, 0b0001_1111);
        bus.Write(0xFFFF, 0b0001_1111);

        var cpu = Cpu.Create(bus);

        Step(cpu, 3);

        // Cpu sees:
        //      halt
        //      ld b, 6
        //      inc b
        AssertCpu(4, new(0, 0x0700, 0, 0, 0, 0x0103, Halted: false), cpu);
    }

    [Fact]
    public void HaltBug_HaltCanLoop()
    {
        var bus = Bus.From([
            0b0111_0110,    // halt
            0b0111_0110,    // halt
        ]);
        bus.EnsureSize(0x1_0000);
        bus.Write(0xFF0F, 0b0001_1111);
        bus.Write(0xFFFF, 0b0001_1111);

        var cpu = Cpu.Create(bus);

        Step(cpu, 10);

        AssertCpu(10, new(0, 0, 0, 0, 0, 0x0101, Halted: false), cpu);
    }

    [Fact]
    public void HaltBug_EnableInterrupt_Halt_WillReturnToHalt()
    {
        var bus = Bus.From([
            0b1111_1011,        // ei
            0b0111_0110,        // halt
        ]);
        bus.EnsureSize(0x1_0000);
        bus.Write(0xFF0F, 0b0000_0001);
        bus.Write(0xFFFF, 0b0000_0001);
        bus.Write(0x0040, 0b1100_1001);     // Return the interrupt handler immediately
        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0, 0, 0, 2, 0x0100));

        Step(cpu, 4);
        AssertCpu(11, new(0, 0, 0, 0, 2, 0x0101), cpu);
        Assert.Equal(0x01, bus.Read(0));
        Assert.Equal(0x01, bus.Read(1));
    }
}
