namespace Boii.IO.Test;

public class CartridgeTest
{
    [Fact]
    public void FromFile_WithDemoFile_LoadsCorrectly()
    {
        var demoPath = "roms/cpu_instrs.gb";

        var (_, errors) = Cartridge.FromFile(demoPath, new());

        Assert.Empty(errors);
    }

    [Fact]
    public void FromFile_WithDemoFile_FailsGlobalChecksum()
    {
        var demoPath = "roms/cpu_instrs.gb";
    
        var (_, errors) = Cartridge.FromFile(demoPath, new() { CheckGlobalChecksum = true });

        Assert.Single(errors);
    }
}
