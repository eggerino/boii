using System;
using System.Linq;
using Boii.Abstractions;
using Boii.Errors;

namespace Boii.Graphics;

public class Ppu : ISlaveComponent
{
    private int _previousLine = -1;
    private int _previousMode = -1;
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
        foreach (var _ in Enumerable.Range(0, (int)ticks))
        {
            _dots += 4;
            _dots %= 70_224;

            var currentLine = (int)(_dots / 456);
            var currentMode = (currentLine, _dots % 456) switch
            {
                ( < 144, < 80) => 2,
                ( < 144, < 252) => 3,
                ( < 144, _) => 0,
                _ => 1,
            };

            if (currentLine != _previousLine)
                OnLineChanged(currentLine, currentMode);

            if (currentMode != _previousMode)
                OnModeChanged(currentLine, currentMode);

            _previousLine = currentLine;
            _previousMode = currentMode;
        }
    }

    private void OnLineChanged(int line, int mode)
    {
        _controller.SetCurrentHorizontalLine((byte)line);
    }

    private void OnModeChanged(int line, int mode)
    {
        _controller.SetPpuMode((ushort)mode);

        if (mode == 3)              // Draw line in draw pixel mode
            DrawLine(line);
        else if (mode == 1)         // Show in VBlank mode
            _renderer.Update();
    }

    // Render pipeline
    private void DrawLine(int line)
    {
        Span<Color> row = stackalloc Color[160];
        row.Clear();

        RenderBackgroundLine(line, row);
        RenderWindowLine(line, row);
        RenderObjectsLine(line, row);

        SendRowToRenderer(row, line);
    }

    private void RenderBackgroundLine(int line, Span<Color> row)
    {
        if (!_controller.IsWindowAndBackgroudEnabled)
            return;

        var getTileIndex = BackgroundGetTileIndexSelector();
        var getTile = GetWindowOrBackgroundTileSelector();
        RenderBackgroundOrWindowLine(line, getTileIndex, getTile, _controller.BackgroundViewportPositionX, _controller.BackgroundViewportPositionY, row);
    }

    private void RenderWindowLine(int line, Span<Color> row)
    {
        if (!_controller.IsWindowAndBackgroudEnabled)
            return;

        if (!_controller.IsWindowEnabled)
            return;

        var getTileIndex = WindowGetTileIndexSelector();
        var getTile = GetWindowOrBackgroundTileSelector();
        RenderBackgroundOrWindowLine(line, getTileIndex, getTile, _controller.WindowPositionX, _controller.WindowPositionY, row);
    }

    private void RenderBackgroundOrWindowLine(int line, Func<int, int, byte> getTileIndex, Action<byte, Span<byte>> getTile,
        int positionX, int positionY, Span<Color> row)
    {
        // Get all color ids in the row
        Span<byte> colorIds = stackalloc byte[32 * 8];
        var y = (line + positionY) % 256; // Warp around
        var yMap = y / 8;
        var yTile = y - yMap * 8;

        Span<byte> tile = stackalloc byte[Tile.Size];
        foreach (var xMap in Enumerable.Range(0, 32))
        {
            var tileIndex = getTileIndex(xMap, yMap);
            getTile(tileIndex, tile);

            foreach (var xTile in Enumerable.Range(0, 8))
            {
                colorIds[8 * xMap + xTile] = (byte)Tile.GetColorId(tile, xTile, yTile);
            }
        }

        // Get all colors of pixels in viewport
        foreach (var i in Enumerable.Range(0, 160))
        {
            var colorIdIndex = (i + positionX) % 256; // Wrap around
            row[i] = _controller.GetColor(colorIds[colorIdIndex]);
        }
    }

    private void RenderObjectsLine(int line, Span<Color> row)
    {
        if (!_controller.AreObjectsEnabled)
            return;

        var getColorIds = GetColorIdsSelector();
        var ySize = _controller.ObjectSize == ObjectSizeKind.Pixel8x16 ? 16 : 8;

        // Removed hardware limitation of only 10 drawable objects
        // Render every object that is visible in the line
        Span<byte> objectAttributes = stackalloc byte[ObjectAttribute.Size];
        Span<int> colorIds = stackalloc int[8];
        foreach (var objectIndex in Enumerable.Range(0, 40))
        {
            _oam.GetObjectAttributes(objectIndex, objectAttributes);

            // Determine if the object is in the viewport
            if (!ObjectAttribute.GetPriorityFlag(objectAttributes))
                continue;                                               // Object should is disabled

            var x = ObjectAttribute.GetXPosition(objectAttributes);
            var y = ObjectAttribute.GetYPosition(objectAttributes);
            if (x <= -8 || x >= 160)
                continue;                                               // Object is outside of viewport
            if (line < y || (y + ySize) <= line)
                continue;                                               // Object is not on the line

            var yTile = line - y;
            getColorIds(objectAttributes, yTile, colorIds);

            var xsTile = Enumerable.Range(0, 8);
            if (ObjectAttribute.GetXFlipFlag(objectAttributes))
                xsTile = xsTile.Reverse();

            var getObjectColor = GetObjectColorSelector(objectAttributes);
            foreach (var (xTile, i) in xsTile.Select((xTile, i) => (xTile, i)))
            {
                var colorId = colorIds[xTile];
                if (colorId == 0)           // 0 means transparent
                    continue;               // Pixel is tranparent

                var color = getObjectColor(colorId);
                var xDest = x + i;

                if (xDest < 0 || xDest >= 256)
                    continue;               // Pixel is outside of viewport

                row[xDest] = color;
            }
        }
    }

    void SendRowToRenderer(ReadOnlySpan<Color> row, int line)
    {
        foreach (var x in Enumerable.Range(0, 160))
        {
            _renderer.SetPixel(x, line, row[x]);
        }
    }

    // Get row of color ids of a specific object's tile
    private void GetColorIds8(ReadOnlySpan<byte> objectAttributes, int dy, Span<int> colorIds)
    {
        Span<byte> tile = stackalloc byte[Tile.Size];
        var tileIndex = ObjectAttribute.GetSingleTileIndex(objectAttributes);
        _vram.TileData.GetObjectTile((byte)tileIndex, tile);

        var y = ObjectAttribute.GetYFlipFlag(objectAttributes) ? 7 - dy : dy;
        foreach (var x in Enumerable.Range(0, 8))
        {
            colorIds[x] = Tile.GetColorId(tile, x, y);
        }
    }

    private void GetColorIds16(ReadOnlySpan<byte> objectAttributes, int dy, Span<int> colorIds)
    {
        var useBottomTile = dy > 7;
        var y = dy > 7 ? dy - 8 : dy;

        Span<byte> tile = stackalloc byte[Tile.Size];
        var tileIndices = ObjectAttribute.GetDoubleTilesIndex(objectAttributes);
        if (ObjectAttribute.GetYFlipFlag(objectAttributes))
        {
            useBottomTile = !useBottomTile;
            y = 7 - y;
        }

        var tileIndex = useBottomTile ? tileIndices.bottom : tileIndices.top;
        _vram.TileData.GetObjectTile((byte)tileIndex, tile);

        foreach (var x in Enumerable.Range(0, 8))
        {
            colorIds[x] = Tile.GetColorId(tile, x, y);
        }
    }

    // Method selector controlled by flags
    private Func<int, int, byte> BackgroundGetTileIndexSelector() => _controller.BackgroundTileMapArea switch
    {
        TileMapAreaKind.First => _vram.TileMaps.GetTileIndexFromFirst,
        TileMapAreaKind.Second => _vram.TileMaps.GetTileIndexFromSecond,
        var x => throw PatternMatchingError.Create(x),
    };

    private Func<int, int, byte> WindowGetTileIndexSelector() => _controller.WindowTileMapArea switch
    {
        TileMapAreaKind.First => _vram.TileMaps.GetTileIndexFromFirst,
        TileMapAreaKind.Second => _vram.TileMaps.GetTileIndexFromSecond,
        var x => throw PatternMatchingError.Create(x),
    };

    private Action<byte, Span<byte>> GetWindowOrBackgroundTileSelector() => _controller.WindowAndBackgroundTileArea switch
    {
        TileDataAddressingMode.Block0 => _vram.TileData.GetWindowOrBackgroundTileBlock0,
        TileDataAddressingMode.Block2 => _vram.TileData.GetWindowOrBackgroundTileBlock2,
        var x => throw PatternMatchingError.Create(x),
    };

    private Action<ReadOnlySpan<byte>, int, Span<int>> GetColorIdsSelector() => _controller.ObjectSize switch
    {
        ObjectSizeKind.Pixel8x8 => GetColorIds8,
        ObjectSizeKind.Pixel8x16 => GetColorIds16,
        var x => throw PatternMatchingError.Create(x),
    };

    private Func<int, Color> GetObjectColorSelector(ReadOnlySpan<byte> objectAttributes) => ObjectAttribute.GetDmgPalletFlag(objectAttributes) switch
    {
        false => _controller.GetObjectColor0,
        true => _controller.GetObjectColor1,
    };
}
