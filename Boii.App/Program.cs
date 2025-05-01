using Boii.IO;
using Boii.Memory;
using Boii.Processing;

// Args parsing
if (args.Length < 1)
{
    Console.Error.WriteLine($"[Usage] {System.Diagnostics.Process.GetCurrentProcess().ProcessName} <ROM-FILE>");
    Environment.Exit(1);
}
var romPath = args[0];

// Setup the components
var (cart, errors) = Cartridge.FromFile(romPath, new());
if (cart is null)
{
    Console.Error.WriteLine($"[ERR] Could not read given rom file {romPath}");
    foreach (var error in errors)
    {
        Console.Error.Write("- ");
        Console.Error.WriteLine(error);
    }
    Environment.Exit(1);
}

var bus = Bus.Create(
    cartridgeRom: cart.ReadOnlyMemory,
    vram: ArrayMemory.Create("VRAM", 0x2000),
    cartridgeRam: cart.RandomAccessMemory,
    objectAttributeMemory: ArrayMemory.Create("VRAM", 0x2000),
    ioRegisters: ArrayMemory.Create("VRAM", 0x2000));

var cpu = Cpu.Create(bus);

// Run the emulated hardware
while (true)
{
    cpu.Step();
}
