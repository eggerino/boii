using System;
using System.Linq;
using Boii.Abstractions;

namespace Boii.Graphics;

public class Ppu : ISlaveComponent
{
    private ulong _dots = 0;    // 4 dots = 1 tick = 1 M-Cycle
    private readonly LcdController _controller;
    private readonly IRenderer _renderer;
    private readonly VideoRandomAccessMemory _vram;
    private readonly ObjectAttributeMemory _oam;

    private Ppu(LcdController controller, IRenderer renderer, VideoRandomAccessMemory vram, ObjectAttributeMemory oam) =>
        (_controller, _renderer, _vram, _oam) = (controller, renderer, vram, oam);

    public static Ppu Create(LcdController io, IRenderer renderer, VideoRandomAccessMemory vram, ObjectAttributeMemory oam) => new(io, renderer, vram, oam);

    public void Advance(ulong ticks)
    {
        var previousLine = byte.MaxValue;
        var previousMode = -1;

        foreach (var _ in Enumerable.Range(0, (int)ticks))
        {
            _dots += 4;
            _dots %= 70_224;

            var currentLine = (byte)(_dots / 456);
            var currentMode = (currentLine, _dots % 456) switch
            {
                (<144, <80) => 2,
                (<144, <252) => 3,
                (<144, _) => 0,
                _ => 1,
            };

            if (currentLine != previousLine)
                _controller.SetCurrentHorizontalLine(currentLine);  // On line change
            
            if (currentMode != previousMode)
                _controller.SetPpuMode((ushort)currentMode);        // On mode change

            if (currentMode != previousMode && currentMode == 3)
                RenderLine(currentLine);                    // On entering mode 3 (draw pixels)

            previousLine = currentLine;
            previousMode = currentMode;
        }
    }

    void RenderLine(byte line)
    {
        throw new NotImplementedException("[TODO] Implemented rendering logic");
    }
}
