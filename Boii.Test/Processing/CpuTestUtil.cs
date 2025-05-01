namespace Boii.Processing.Test;

public static class CpuTestUtil
{
    public static void Step(Cpu cpu, int amount)
    {
        foreach (var _ in Enumerable.Repeat(0, amount))
            cpu.Step();
    }

    public static void AssertCpu(ulong expectedTicks, Cpu.RegisterState expectedState, Cpu actual)
    {
        Assert.Equal(expectedTicks, actual.Ticks);
        Assert.Equal(expectedState, actual.GetRegisterState());
    }
}
