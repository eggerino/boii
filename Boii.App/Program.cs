using Boii.Graphics;
using Boii.IO;
using Boii.Processing;
using Boii.Raylib;

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

var window = Window.Create($"boii - {cart.Header.Title}", 5, new());

var vram = VideoRandomAccessMemory.Create();
var objectAttributeMemory = ObjectAttributeMemory.Create();
var lcdController = LcdController.Create(bus);
var ppu = Ppu.Create(lcdController, window, vram, objectAttributeMemory);
var cpu = Cpu.Create(bus);

// Inject the components into the global memory bus
bus.CartridgeRom = cart.ReadOnlyMemory;
bus.VideoRam = vram;
bus.CartridgeRam = cart.RandomAccessMemory;
bus.ObjectAttributeMemory = objectAttributeMemory;
// bus.IoRegisters = ioController;

// Run the emulated hardware
window.Open();
try
{
    while (!window.ShouldClose())
    {
        var ticks = cpu.Step();
        // ioController.Advance(ticks);
        ppu.Advance(ticks);
    }
}
finally
{
    window.Close();
}
