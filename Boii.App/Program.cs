
var vram = VideoRandomAccessMemory.Create();
var objectAttributeMemory = ObjectAttributeMemory.Create();
var lcdController = LcdController.Create(bus);
var ppu = Ppu.Create(lcdController, window, vram, objectAttributeMemory);

// Inject the components into the global memory bus
bus.VideoRam = vram;
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
