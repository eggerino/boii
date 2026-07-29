using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Boii.Abstractions;
using Boii.Memory;
using Boii.Util;

namespace Boii.IO;

public class Cartridge
{
    private const int NintendoLogoPointer = 0x0104;
    private const int TitlePointer = 0x0134;
    private const int NewLicenseeCodePointer = 0x0144;
    private const int SgbFlagPointer = 0x0146;
    private const int CartridgeTypePointer = 0x0147;
    private const int RomSizePointer = 0x0148;
    private const int RamSizePointer = 0x0149;
    private const int DestinationCodePointer = 0x014A;
    private const int OldLicenseeCodePointer = 0x014B;
    private const int RomVersionPointer = 0x014C;
    private const int HeaderChecksumPointer = 0x014D;
    private const int GlobalChecksumPointer = 0x014E;

    private const int HeaderSize = 0x0150;
    private const int NintendoLogoSize = 0x30;
    private const int TitleSize = 0x10;
    private const int NewLicenseeCodeSize = 0x02;

    public class ValidationSettings
    {
        public bool CheckRomSize { get; init; } = true;
        public bool CheckNintendoLogo { get; init; } = true;
        public bool CheckHeaderChecksum { get; init; } = true;
        public bool CheckGlobalChecksum { get; init; } = false;
    }

    public record HeaderInfo(
        string Title,
        string NewLicenseeCode,
        byte SgbFlag,
        byte CartridgeType,
        int RomSize,
        int RamSize,
        byte DestinationCode,
        byte OldLicenseeCode,
        byte RomVersion,
        byte HeaderChecksum,
        ushort GlobalChecksum);

    public IGenericIO ReadOnlyMemory { get; }
    public IGenericIO RandomAccessMemory { get; }
    public HeaderInfo Header { get; }

    private Cartridge(IGenericIO readOnlyMemory, IGenericIO randomAccessMemory, HeaderInfo header) =>
        (ReadOnlyMemory, RandomAccessMemory, Header) = (readOnlyMemory, randomAccessMemory, header);

    public static (Cartridge? cartridge, IReadOnlyList<string> errors) FromFile(string path, ValidationSettings settings)
    {
        var romBytes = File.ReadAllBytes(path);

        if (ParseHeader(romBytes) is not HeaderInfo header)
            return (null, ["no header in rom"]);

        var errors = ValidateHeader(romBytes, header, settings);
        if (errors.Count > 0)
            return (null, errors);

        return (new Cartridge(
            readOnlyMemory: ArrayMemory.From($"{nameof(Cartridge)}.{nameof(ReadOnlyMemory)}", romBytes),
            randomAccessMemory: ArrayMemory.Create($"{nameof(Cartridge)}.{nameof(RandomAccessMemory)}", header.RamSize),
            header: header),
            []);
    }
}
