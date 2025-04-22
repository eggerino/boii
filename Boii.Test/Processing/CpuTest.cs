using Boii.Test.Mock;

namespace Boii.Processing.Test;

public class CpuTest
{
    [Fact]
    public void Nop_DoesNothing()
    {
        var bus = Bus.From([0x00]);
        var cpu = Cpu.Create(bus);
        var expected = new Cpu.RegisterDump(0, 0, 0, 0, 0, 0x0101);

        cpu.Step();
        var actual = cpu.GetRegisterDump();

        Assert.Equal(expected, actual);
    }
}
