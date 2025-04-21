using Boii.Abstractions;
using Boii.Errors;

namespace Boii.IO;

public class Bus : IGenericIO
{
    private const int CartridgeRomStart = 0x0000;
    private const int CartridgeRomEnd = 0x8000;

    private const int CartridgeRamStart = 0xA000;
    private const int CartridgeRamEnd = 0xC000;

    private readonly IReadable _cartridgeRom;
    private readonly IGenericIO _cartridgeRam;

    private Bus(IReadable cartridgeRom, IGenericIO cartridgeRam) =>
        (_cartridgeRom, _cartridgeRam) = (cartridgeRom, cartridgeRam);

    public static Bus Create(IReadable cartridgeRom, IGenericIO cartridgeRam) => new(cartridgeRom, cartridgeRam);

    public byte Read(ushort address) => address switch
    {
        var x when CartridgeRomStart <= x && x < CartridgeRomEnd => _cartridgeRom.Read((ushort)(x - CartridgeRomStart)),
        var x when CartridgeRamStart <= x && x < CartridgeRamEnd => _cartridgeRam.Read((ushort)(x - CartridgeRamStart)),
        _ => throw SegmentationFault.Create($"{nameof(Bus)}.{nameof(Read)}", address),
    };

    public void Write(ushort address, byte value)
    {
        if (CartridgeRamStart <= address && address < CartridgeRamEnd)
            _cartridgeRam.Write((ushort)(address - CartridgeRamStart), value);
        else
            throw SegmentationFault.Create($"{nameof(Bus)}.{nameof(Write)}", address);
    }
}
