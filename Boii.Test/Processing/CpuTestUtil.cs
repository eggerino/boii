namespace Boii.Processing.Test;

public static class CpuTestUtil
{
    public static ulong Step(Cpu cpu, int amount)
    {
        var ticks = 0ul;
        foreach (var _ in Enumerable.Repeat(0, amount))
            ticks += cpu.Step();
        return ticks;
    }

    public static void AssertCpu(ulong expectedTicks, Cpu.RegisterState expectedState, Cpu actual)
    {
        Assert.Equal(expectedTicks, actual.Ticks);
        Assert.Equal(expectedState, actual.GetRegisterState());
    }
}
