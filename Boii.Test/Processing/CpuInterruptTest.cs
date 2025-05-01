using Boii.Test.Mock;
using static Boii.Processing.Test.CpuTestUtil;

namespace Boii.Processing.Test;

public class CpuInterruptTest
{
    [Theory]
    [InlineData(0b0000_0001, 0x0040)]
    [InlineData(0b0000_0010, 0x0048)]
    [InlineData(0b0000_0100, 0x0050)]
    [InlineData(0b0000_1000, 0x0058)]
    [InlineData(0b0001_0000, 0x0060)]
    public void InterruptCalls(byte interruptMask, ushort targetAddress)
    {
        var bus = Bus.From([]);
        bus.EnsureSize(0x1_0000);

        bus.Write(0xFF0F, interruptMask);   // Prepare the interrupt
        bus.Write(0xFFFF, interruptMask);
        bus.Write(targetAddress, 0b0000_0100);  // inc b in handler

        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0, 0, 0, 2, 0x0100, true));

        Step(cpu, 2);

        AssertCpu(6, new(0, 0x0100, 0, 0, 0, (ushort)(targetAddress + 1), false), cpu);
        Assert.Equal(0, bus.Read(0xFF0F));
        Assert.Equal(0x00, bus.Read(0));
        Assert.Equal(0x01, bus.Read(1));
    }

    [Fact]
    public void InterruptPriority()
    {
        var bus = Bus.From([0b1111_1011]);  // ei
        bus.EnsureSize(0x1_0000);

        bus.Write(0xFF0F, 0b0001_1111); // prepare all interrupts
        bus.Write(0xFFFF, 0b0001_1111);
        bus.Write(0x40, 0b1111_1011);  // ie all handlers
        bus.Write(0x48, 0b1111_1011);  // ie all handlers
        bus.Write(0x50, 0b1111_1011);  // ie all handlers
        bus.Write(0x58, 0b1111_1011);  // ie all handlers
        bus.Write(0x60, 0b1111_1011);  // ie all handlers

        var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0, 0, 0, 10, 0x0100));

        Step(cpu, 3);
        Assert.Equal(0x0040, cpu.GetRegisterState().ProgramCounter);
        Assert.Equal(0b0001_1110, bus.Read(0xFF0F));
        Step(cpu, 3);
        Assert.Equal(0x0048, cpu.GetRegisterState().ProgramCounter);
        Assert.Equal(0b0001_1100, bus.Read(0xFF0F));
        Step(cpu, 3);
        Assert.Equal(0x0050, cpu.GetRegisterState().ProgramCounter);
        Assert.Equal(0b0001_1000, bus.Read(0xFF0F));
        Step(cpu, 3);
        Assert.Equal(0x0058, cpu.GetRegisterState().ProgramCounter);
        Assert.Equal(0b0001_0000, bus.Read(0xFF0F));
        Step(cpu, 3);
        Assert.Equal(0x0060, cpu.GetRegisterState().ProgramCounter);
        Assert.Equal(0b0000_0000, bus.Read(0xFF0F));
    }
}