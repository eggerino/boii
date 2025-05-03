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

    public IGenericIO? CartridgeRom { get; set; }
    public IGenericIO? VideoRam { get; set; }
    public IGenericIO? CartridgeRam { get; set; }
    private readonly ArrayMemory _workRam = ArrayMemory.Create("WRAM", 0x2000);
    public IGenericIO? ObjectAttributeMemory { get; set; }
    public IGenericIO? IoRegisters { get; set; }
    private readonly ArrayMemory _highRam = ArrayMemory.Create("HRAM", 0x007F);
    private byte _interruptEnable = 0;

    private Bus() { }

    public static Bus CreateWithoutLinks() => new();

    public byte Read(ushort address) => address switch
    {
        var x when CartridgeRomRange.IsIn(x, out var i) && CartridgeRom is not null => CartridgeRom.Read(i),
        var x when VramRange.IsIn(x, out var i) && VideoRam is not null => VideoRam.Read(i),
        var x when CartridgeRamRange.IsIn(x, out var i) && CartridgeRam is not null => CartridgeRam.Read(i),
        var x when WorkRamRange.IsIn(x, out var i) => _workRam.Read(i),
        var x when EchoRamRange.IsIn(x, out var i) => _workRam.Read(i),     // Mirrors wram
        var x when ObjectAttributeMemoryRange.IsIn(x, out var i) && ObjectAttributeMemory is not null => ObjectAttributeMemory.Read(i),
        var x when IoRegistersRange.IsIn(x, out var i) && IoRegisters is not null => IoRegisters.Read(i),
        var x when HighRamRange.IsIn(x, out var i) => _highRam.Read(i),
        0xFFFF => _interruptEnable,
        _ => throw SegmentationFault.Create($"{nameof(Bus)}.{nameof(Read)}", address),
    };

    public void Write(ushort address, byte value)
    {
        if (CartridgeRomRange.IsIn(address, out var x) && CartridgeRom is not null)
            CartridgeRom.Write(x, value);
        else if (VramRange.IsIn(address, out x) && VideoRam is not null)
            VideoRam.Write(x, value);
        else if (CartridgeRamRange.IsIn(address, out x) && CartridgeRam is not null)
            CartridgeRam.Write(x, value);
        else if (WorkRamRange.IsIn(address, out x))
            _workRam.Write(x, value);
        else if (EchoRamRange.IsIn(address, out x))
            _workRam.Write(x, value);
        else if (ObjectAttributeMemoryRange.IsIn(address, out x) && ObjectAttributeMemory is not null)
            ObjectAttributeMemory.Write(x, value);
        else if (IoRegistersRange.IsIn(address, out x) && IoRegisters is not null)
            IoRegisters.Write(x, value);
        else if (HighRamRange.IsIn(address, out x))
            _highRam.Write(x, value);
        else if (address == 0xFFFF)
            _interruptEnable = value;
        else
            throw SegmentationFault.Create($"{nameof(Bus)}.{nameof(Write)}", address);
    }
}
