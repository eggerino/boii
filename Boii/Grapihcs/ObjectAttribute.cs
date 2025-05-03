using System;
using Boii.Util;

namespace Boii.Graphics;

public static class ObjectAttribute
{
    public const int Size = 4;

    public static int GetYPosition(this ReadOnlySpan<byte> objectAttribute) => objectAttribute[0] - 16;
    
    public static int GetXPosition(this ReadOnlySpan<byte> objectAttribute) => objectAttribute[1] - 8;

    public static int GetSingleTileIndex(this ReadOnlySpan<byte> objectAttribute) => objectAttribute[2];
    
    public static (int top, int bottom) GetDoubleTilesIndex(this ReadOnlySpan<byte> objectAttribute)
    {
        var bottom = objectAttribute[2] | 1;
        var top = bottom - 1;
        return (top, bottom);
    }

    public static bool GetPriorityFlag(this ReadOnlySpan<byte> objectAttribute) => BinaryUtil.GetBit(objectAttribute[3], 7);

    public static bool GetYFlipFlag(this ReadOnlySpan<byte> objectAttribute) => BinaryUtil.GetBit(objectAttribute[3], 6);
    
    public static bool GetXFlipFlag(this ReadOnlySpan<byte> objectAttribute) => BinaryUtil.GetBit(objectAttribute[3], 5);

    public static bool GetDmgPalletFlag(this ReadOnlySpan<byte> objectAttribute) => BinaryUtil.GetBit(objectAttribute[3], 4);
}
