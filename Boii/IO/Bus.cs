using Boii.Abstractions;
using Boii.Errors;
using Boii.Memory;

namespace Boii.IO;

public class Bus : IGenericIO
{
    private readonly record struct Range(ushort Lower, ushort Upper)
    {
        public bool IsIn(ushort address, out ushort relativeAddress)
        {
            relativeAddress = (ushort)(address - Lower);
            return Lower <= address && address < Upper;
        }
    }

    private static readonly Range CartridgeRomRange = new(0x0000, 0x8000);
    private static readonly Range VramRange = new(0x8000, 0xA000);
    private static readonly Range CartridgeRamRange = new(0xA000, 0xC000);
    private static readonly Range WorkRamRange = new(0xC000, 0xE000);
    private static readonly Range EchoRamRange = new(0xE000, 0xFE00);
    private static readonly Range ObjectAttributeMemoryRange = new(0xFE00, 0xFEA0);
    private static readonly Range IoRegistersRange = new(0xFF00, 0xFF80);
    private static readonly Range HighRamRange = new(0xFF80, 0xFFFF);

    private readonly IGenericIO _cartridgeRom;
    private readonly IGenericIO _vram;
    private readonly IGenericIO _cartridgeRam;
    private readonly ArrayMemory _workRam = ArrayMemory.Create("WRAM", 0x2000);
    private readonly IGenericIO _objectAttributeMemory;
    private readonly IGenericIO _ioRegisters;
    private readonly ArrayMemory _highRam = ArrayMemory.Create("HRAM", 0x007F);
    private byte _interruptEnable = 0;

    private Bus(IGenericIO cartridgeRom, IGenericIO vram, IGenericIO cartridgeRam, IGenericIO objectAttributeMemory, IGenericIO ioRegisters)
    {
        _cartridgeRom = cartridgeRom;
        _vram = vram;
        _cartridgeRam = cartridgeRam;
        _objectAttributeMemory = objectAttributeMemory;
        _ioRegisters = ioRegisters;
    }

    public static Bus Create(IGenericIO cartridgeRom, IGenericIO vram, IGenericIO cartridgeRam, IGenericIO objectAttributeMemory, IGenericIO ioRegisters) =>
        new(cartridgeRom, vram, cartridgeRam, objectAttributeMemory, ioRegisters);

    public byte Read(ushort address) => address switch
    {
        var x when CartridgeRomRange.IsIn(x, out var i) => _cartridgeRom.Read(i),
        var x when VramRange.IsIn(x, out var i) => _vram.Read(i),
        var x when CartridgeRamRange.IsIn(x, out var i) => _cartridgeRam.Read(i),
        var x when WorkRamRange.IsIn(x, out var i) => _workRam.Read(i),
        var x when EchoRamRange.IsIn(x, out var i) => _workRam.Read(i),     // Mirrors wram
        var x when ObjectAttributeMemoryRange.IsIn(x, out var i) => _objectAttributeMemory.Read(i),
        var x when IoRegistersRange.IsIn(x, out var i) => _ioRegisters.Read(i),
        var x when HighRamRange.IsIn(x, out var i) => _highRam.Read(i),
        0xFFFF => _interruptEnable,
        _ => throw SegmentationFault.Create($"{nameof(Bus)}.{nameof(Read)}", address),
    };

    public void Write(ushort address, byte value)
    {
        if (CartridgeRomRange.IsIn(address, out var x))
            _cartridgeRom.Write(x, value);
        else if (VramRange.IsIn(address, out x))
            _vram.Write(x, value);
        else if (CartridgeRamRange.IsIn(address, out x))
            _cartridgeRam.Write(x, value);
        else if (WorkRamRange.IsIn(address, out x))
            _workRam.Write(x, value);
        else if (EchoRamRange.IsIn(address, out x))
            _workRam.Write(x, value);
        else if (ObjectAttributeMemoryRange.IsIn(address, out x))
            _objectAttributeMemory.Write(x, value);
        else if (IoRegistersRange.IsIn(address, out x))
            _ioRegisters.Write(x, value);
        else if (HighRamRange.IsIn(address, out x))
            _highRam.Write(x, value);
        else if (address == 0xFFFF)
            _interruptEnable = value;
        else
            throw SegmentationFault.Create($"{nameof(Bus)}.{nameof(Write)}", address);
    }
}
