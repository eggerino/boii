using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Boii.Abstractions;
using Boii.Errors;
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

    private class Memory(string location, byte[] data) : IGenericIO
    {
        public byte Read(ushort address)
        {
            if (address >= data.Length)
                throw SegmentationFault.Create($"{location}", address);
            return data[address];
        }

        public void Write(ushort address, byte value)
        {
            if (address >= data.Length)
                throw SegmentationFault.Create($"{location}", address);
            data[address] = value;
        }
    }

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

        var ramBytes = new byte[header.RamSize];

        return (new Cartridge(
            readOnlyMemory: new Memory($"{nameof(Cartridge)}.{nameof(ReadOnlyMemory)}", romBytes),
            randomAccessMemory: new Memory($"{nameof(Cartridge)}.{nameof(RandomAccessMemory)}", ramBytes), header), []);
    }

    private static HeaderInfo? ParseHeader(byte[] romBytes)
    {
        if (romBytes.Length < HeaderSize)
            return null;

        if (GetRomSize(romBytes[RomSizePointer]) is not int romSize)
            return null;

        if (GetRamSize(romBytes[RamSizePointer]) is not int ramSize)
            return null;

        return new HeaderInfo(
            Title: Encoding.ASCII.GetString(romBytes, TitlePointer, TitleSize),
            NewLicenseeCode: Encoding.ASCII.GetString(romBytes, NewLicenseeCodePointer, NewLicenseeCodeSize),
            SgbFlag: romBytes[SgbFlagPointer],
            CartridgeType: romBytes[CartridgeTypePointer],
            RomSize: romSize,
            RamSize: ramSize,
            DestinationCode: romBytes[DestinationCodePointer],
            OldLicenseeCode: romBytes[OldLicenseeCodePointer],
            RomVersion: romBytes[RomVersionPointer],
            HeaderChecksum: romBytes[HeaderChecksumPointer],
            GlobalChecksum: BinaryUtil.ToUShort(high: romBytes[GlobalChecksumPointer], low: romBytes[GlobalChecksumPointer + 1])
        );
    }

    private static int? GetRomSize(byte value) => value switch
    {
        var x when 0 <= x && x <= 8 => 32 * 1024 * (1 << x),
        0x52 => (int)(1.1 * 1024 * 1024),
        0x53 => (int)(1.2 * 1024 * 1024),
        0x54 => (int)(1.5 * 1024 * 1024),
        _ => null,
    };

    private static int? GetRamSize(byte value) => value switch
    {
        0 => 0,
        2 => 8 * 1024,
        3 => 32 * 1024,
        4 => 128 * 1024,
        5 => 64 * 1024,
        _ => null,
    };

    private static List<string> ValidateHeader(byte[] romBytes, HeaderInfo header, ValidationSettings settings)
    {
        var errors = new List<string>();

        if (settings.CheckRomSize && romBytes.Length != header.RomSize)
            errors.Add($"inconsistent rom size (actual={romBytes.Length}, header={header.RomSize})");

        if (settings.CheckNintendoLogo && !HasNintendoLogo(romBytes))
            errors.Add("rom does not have the nintendo logo");

        var headerChecksum = ComputeHeaderChecksum(romBytes);
        if (settings.CheckHeaderChecksum && headerChecksum != header.HeaderChecksum)
            errors.Add($"invalid header checksum (actual={headerChecksum}, header={header.HeaderChecksum})");

        var globalChecksum = ComputeGlobalChecksum(romBytes);
        if (settings.CheckGlobalChecksum && globalChecksum != header.GlobalChecksum)
            errors.Add($"invalid global checksum (actual={globalChecksum}, header={header.GlobalChecksum})");

        return errors;
    }

    private static bool HasNintendoLogo(byte[] romBytes)
    {
        ReadOnlySpan<byte> logo =
        [
            0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D,
            0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99,
            0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E,
        ];

        return romBytes
            .AsSpan(NintendoLogoPointer, NintendoLogoSize)
            .SequenceEqual(logo);
    }

    private static byte ComputeHeaderChecksum(byte[] romBytes)
    {
        byte checksum = 0;
        for (var address = 0x0134; address <= 0x014C; address++)
        {
            checksum = (byte)(checksum - romBytes[address] - 1);
        }
        return checksum;
    }

    private static ushort ComputeGlobalChecksum(byte[] romBytes)
    {
        ushort checksum = 0;
        foreach (var romByte in romBytes)
        {
            checksum += romByte;
        }
        checksum -= romBytes[GlobalChecksumPointer];
        checksum -= romBytes[GlobalChecksumPointer - 1];
        return checksum;
    }
}
