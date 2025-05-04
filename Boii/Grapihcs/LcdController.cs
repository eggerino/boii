using System.Linq;
using Boii.Abstractions;
using Boii.Memory;
using Boii.Timing;
using Boii.Util;

namespace Boii.Graphics;

public class LcdController : IGenericIO, ISlaveComponent
{
    private readonly byte[] _buffer = new byte[12];
    private int _previousOamDmaSource = 0;
    private GenericTimer _oamDmaSourceTimer = GenericTimer.Create();

    private readonly IGenericIO _bus;

    private LcdController(IGenericIO bus) => _bus = bus;

    public static LcdController Create(IGenericIO bus) => new(bus);

    public byte Read(ushort address) => BufferAccesser.Read(_buffer, address, "LcdIo");

    public void Write(ushort address, byte value) => BufferAccesser.Write(_buffer, address, value, "LcdIo");

    public void Advance(ulong ticks)
    {
        _oamDmaSourceTimer.Advance(ticks);

        // OAM DMA Transfer
        if (OmaDmaTransferSource != _previousOamDmaSource)
        {
            // Start transfer
            _previousOamDmaSource = OmaDmaTransferSource;
            _oamDmaSourceTimer.Start(160);
        }

        if (_oamDmaSourceTimer.Done)
        {
            // Run the transfer
            _oamDmaSourceTimer.Disable();
            foreach (var i in Enumerable.Range(0, 0xA0))
            {
                var value = _bus.Read((ushort)(_previousOamDmaSource << 8 | i));
                _bus.Write((ushort)(0xFE00 + i), value);
            }
        }
    }

    // Parsing of the buffer data

    // LCD Control
    public bool IsWindowAndBackgroudEnabled => BinaryUtil.GetBit(_buffer[0], 0);
    public bool AreObjectsEnabled => BinaryUtil.GetBit(_buffer[0], 1);
    public ObjectSizeKind ObjectSize => BinaryUtil.GetBit(_buffer[0], 2)
        ? ObjectSizeKind.Pixel8x16
        : ObjectSizeKind.Pixel8x8;
    public TileMapAreaKind BackgroundTileMapArea => BinaryUtil.GetBit(_buffer[0], 3)
        ? TileMapAreaKind.Second
        : TileMapAreaKind.First;
    public TileDataAddressingMode WindowAndBackgroundTileArea => BinaryUtil.GetBit(_buffer[0], 4)
        ? TileDataAddressingMode.Block0
        : TileDataAddressingMode.Block2;
    public bool IsWindowEnabled => BinaryUtil.GetBit(_buffer[0], 5);
    public TileMapAreaKind WindowTileMapArea => BinaryUtil.GetBit(_buffer[0], 6)
        ? TileMapAreaKind.Second
        : TileMapAreaKind.First;
    public bool IsLcdAndPpuEnabled => BinaryUtil.GetBit(_buffer[0], 7);

    // STAT
    public void SetPpuMode(ushort value)
    {
        var low = BinaryUtil.GetBit(value, 0);
        var high = BinaryUtil.GetBit(value, 1);

        var stat = _buffer[1];
        stat = BinaryUtil.SetBit(stat, 0, low);
        stat = BinaryUtil.SetBit(stat, 1, high);
        _buffer[1] = stat;

        if (IsMode0Condition && value == 0)
            RequestStatInterrupt();

        if (IsMode1Condition && value == 1)
            RequestStatInterrupt();

        if (IsMode2Condition && value == 2)
            RequestStatInterrupt();
    }
    private void SetHorizontalLineComparison(bool value) => _buffer[1] = BinaryUtil.SetBit(_buffer[1], 2, value);
    private bool IsMode0Condition => BinaryUtil.GetBit(_buffer[1], 3);
    private bool IsMode1Condition => BinaryUtil.GetBit(_buffer[1], 4);
    private bool IsMode2Condition => BinaryUtil.GetBit(_buffer[1], 5);
    private bool IsModeLYCCondition => BinaryUtil.GetBit(_buffer[1], 6);

    // Background viewport
    public byte BackgroundViewportPositionY => _buffer[2];
    public byte BackgroundViewportPositionX => _buffer[3];

    // Horizontal line tracking
    public void SetCurrentHorizontalLine(byte value)
    {
        _buffer[4] = value;
        var isEqual = value == HorizontalLineCompare;
        SetHorizontalLineComparison(isEqual);

        if (IsModeLYCCondition && isEqual)
            RequestStatInterrupt();
    }

    private byte HorizontalLineCompare => _buffer[5];

    // OMA DMA Transfer
    private byte OmaDmaTransferSource => _buffer[6];

    // Color palette
    public Color GetColor(int colorId) => (Color)BinaryUtil.Slice(_buffer[7], 2 * colorId, 2);
    public Color GetObjectColor0(int colorId) => (Color)BinaryUtil.Slice(_buffer[8], 2 * colorId, 2);
    public Color GetObjectColor1(int colorId) => (Color)BinaryUtil.Slice(_buffer[9], 2 * colorId, 2);

    // Window position
    public int WindowPositionY => _buffer[10];
    public int WindowPositionX => _buffer[11] - 7;

    private void RequestStatInterrupt()
    {
        var interruptFlags = _bus.Read(0xFF0F);
        interruptFlags = BinaryUtil.SetBit(interruptFlags, 1, true);
        _bus.Write(0xFF0F, interruptFlags);
    }
}
