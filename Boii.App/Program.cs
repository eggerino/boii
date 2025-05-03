using Boii.Graphics;
using Boii.IO;
using Boii.Processing;

// Args parsing
if (args.Length < 1)
{
    Console.Error.WriteLine($"[Usage] {System.Diagnostics.Process.GetCurrentProcess().ProcessName} <ROM-FILE>");
    Environment.Exit(1);
}
var romPath = args[0];

// Setup the components
var bus = Bus.CreateWithoutLinks();

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

var vram = VideoRandomAccessMemory.Create();
var objectAttributeMemory = ObjectAttributeMemory.Create();


// Inject the components into the global memory bus
bus.CartridgeRom = cart.ReadOnlyMemory;
bus.VideoRam = vram;
bus.CartridgeRam = cart.RandomAccessMemory;
bus.ObjectAttributeMemory = objectAttributeMemory;
// TODO IO registers

var cpu = Cpu.Create(bus);

// Run the emulated hardware
while (true)
{
    cpu.Step();
}
