using System;

namespace Boii.Graphics;

public static class Tile
{
    public const int Size = 16;

    public static int GetColorId(this ReadOnlySpan<byte> tile, int x, int y)
    {
        var low = tile[2 * y];
        var high = tile[2 * y + 1];

        var lowBit = (low >> x) & 1;
        var highBit = (high >> x) & 1;

        return (highBit << 1) | lowBit;
    }
}
