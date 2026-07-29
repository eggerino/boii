const NINTENDO_LOGO_ADDR: usize = 0x0104;
const TITLE_ADDR: usize = 0x0134;
const CGB_FLAG_ADDR: usize = 0x0143;
const NEW_LICENSEE_CODE_ADDR: usize = 0x0144;
const SGB_FLAG_ADDR: usize = 0x0146;
const CARTRIDGE_TYPE_ADDR: usize = 0x0147;
const ROM_SIZE_ADDR: usize = 0x0148;
const RAM_SIZE_ADDR: usize = 0x0149;
const DESTINATION_CODE_ADDR: usize = 0x014A;
const OLD_LICENSEE_CODE_ADDR: usize = 0x014B;
const ROM_VERSION_ADDR: usize = 0x014C;
const HEADER_CHECKSUM_ADDR: usize = 0x014D;
const GLOBAL_CHECKSUM_ADDR: usize = 0x014E;

const HEADER_SIZE: usize = 0x0150;
const TITLE_SIZE: usize = 0x0010;
const NEW_LICENSEE_CODE_SIZE: usize = 0x0002;

const NINTENDO_LOGO_LITERAL: [u8; 0x0030] = [
    0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D,
    0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99,
    0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E,
];

#[derive(Debug, PartialEq)]
pub enum Error {
    NoHeader { rom_size: usize },
    InvalidNintendoLogo(Box<[u8]>),
    InvalidTitleSection(Box<[u8]>),
    InvalidCartridgeType(u8),
    UnknownRomSize(u8),
    UnknownRamSize(u8),
    InvalidDestionationCode(u8),
    MismatchingRomSizes { expected: usize, actual: usize },
    ViolatedHeaderChecksum { expected: u8, actual: u8 },
    ViolatedGlobalChecksum { expected: u16, actual: u16 },
}

pub type Result<T> = core::result::Result<T, Error>;

#[derive(Debug, PartialEq)]
pub enum CgbFlag {
    Monochrom,
    Color,
}

#[derive(Debug, PartialEq)]
pub enum SgbFlag {
    Use,
    Ignore(u8),
}

#[derive(Debug, PartialEq)]
pub enum MemoryBlockControllerType {
    None,
    One,
    Two,
    Three,
    Five,
    Six,
    Seven,
}

#[derive(Debug, PartialEq)]
pub struct CartridgeType {
    pub mbc: MemoryBlockControllerType,
    pub ram: bool,
    pub battery: bool,
    pub mmm01: bool,
    pub timer: bool,
    pub rumble: bool,
    pub sensor: bool,
    pub pocket_camera: bool,
    pub bandai_tama5: bool,
    pub huc1: bool,
    pub huc3: bool,
}

#[derive(Debug, PartialEq)]
pub enum DestinationCode {
    Japan,
    Oversea,
}

#[derive(Debug, PartialEq)]
pub struct Header {
    pub title: String,
    pub cgb_flag: CgbFlag,
    pub licensee: Option<String>,
    pub sgb_flag: SgbFlag,
    pub cartridge_type: CartridgeType,
    pub rom_size: usize,
    pub ram_size: usize,
    pub destination_code: DestinationCode,
    pub rom_version: u8,
    pub header_checksum: u8,
    pub global_checksum: u16,
}

#[derive(Default)]
pub struct ParseConfig {
    pub check_nintento_logo: bool,
    pub check_matching_rom_sizes: bool,
    pub check_header_checksum: bool,
    pub check_global_checksum: bool,
}

impl Header {
    pub fn parse(rom: &[u8], config: &ParseConfig) -> Result<Self> {
        // Always check the header size to safely read from the address of the spec
        check_header_size(rom)?;

        let title = parse_title(rom)?;
        let cgb_flag = parse_cgb_flag(rom);
        let new_licensee = parse_new_licensee_code(rom);
        let sgb_flag = parse_sgb_flag(rom);
        let cartridge_type = parse_cartridge_type(rom)?;
        let rom_size = parse_rom_size(rom)?;
        let ram_size = parse_ram_size(rom)?;
        let destination_code = parse_destincation_code(rom)?;
        let licensee = parse_old_licensee_code(rom, new_licensee);
        let rom_version = parse_rom_version(rom);
        let header_checksum = parse_header_checksum(rom);
        let global_checksum = parse_global_checksum(rom);

        if config.check_nintento_logo {
            check_nintendo_logo(rom)?;
        }

        if config.check_matching_rom_sizes {
            check_rom_size(rom, rom_size)?;
        }

        if config.check_header_checksum {
            check_header_checksum(rom, header_checksum)?;
        }

        if config.check_global_checksum {
            check_global_checksum(rom, global_checksum)?;
        }

        let header = Self {
            title,
            cgb_flag,
            licensee,
            sgb_flag,
            cartridge_type,
            rom_size,
            ram_size,
            destination_code,
            rom_version,
            header_checksum,
            global_checksum,
        };
        Ok(header)
    }
}

fn check_header_size(rom: &[u8]) -> Result<()> {
    if rom.len() < HEADER_SIZE {
        Err(Error::NoHeader {
            rom_size: rom.len(),
        })
    } else {
        Ok(())
    }
}

fn check_nintendo_logo(rom: &[u8]) -> Result<()> {
    let logo = &rom[NINTENDO_LOGO_ADDR..NINTENDO_LOGO_ADDR + NINTENDO_LOGO_LITERAL.len()];
    if logo == NINTENDO_LOGO_LITERAL {
        Ok(())
    } else {
        Err(Error::InvalidNintendoLogo(logo.into()))
    }
}

fn parse_title(rom: &[u8]) -> Result<String> {
    // This area of the rom is interpreted differently in a backwards compatible fashion.
    // Originally it represents the title as an ascii encoded string with trailing zeros as padding.
    // The last byte is later reused as the cgb flag. The 8-bit (which makes this byte an illegal ascii value)
    // is used as a discrimitor, whether the last bytes if the cgb flag or part of the title.
    let raw = &rom[TITLE_ADDR..TITLE_ADDR + TITLE_SIZE];

    let mut title = raw;

    // Chop off the cgb flag if its present
    if title[TITLE_SIZE - 1] & 0b1000_0000 > 0 {
        title = &title[..TITLE_SIZE - 1];
    }

    // Chop off the trailing zero bytes added for padding
    while let Some(x) = title.last()
        && *x == 0
    {
        title = &title[..title.len() - 1];
    }

    str::from_utf8(title)
        .map(String::from)
        .map_err(|_| Error::InvalidTitleSection(raw.into()))
}

fn parse_cgb_flag(rom: &[u8]) -> CgbFlag {
    let flag = rom[CGB_FLAG_ADDR];

    // Check if the flag is part of the title (8-bit is the discrimator, see `parse_title` for more details)
    if flag & 0b1000_0000 == 0 {
        // Original use case as title -> pre cgb
        return CgbFlag::Monochrom;
    }

    // 7-Bit determines the color mode
    if flag & 0b0100_0000 > 0 {
        CgbFlag::Color
    } else {
        CgbFlag::Monochrom
    }
}

fn parse_new_licensee_code(rom: &[u8]) -> Option<String> {
    let code = str::from_utf8(
        &rom[NEW_LICENSEE_CODE_ADDR..NEW_LICENSEE_CODE_ADDR + NEW_LICENSEE_CODE_SIZE],
    )
    .ok()?;

    let name = match code {
        "01" => "Nintendo Research & Development 1",
        "08" => "Capcom",
        "13" => "EA (Electronic Arts)",
        "18" => "Hudson Soft",
        "19" => "B-AI",
        "20" => "KSS",
        "22" => "Planning Office WADA",
        "24" => "PCM Complete",
        "25" => "San-X",
        "28" => "Kemco",
        "29" => "SETA Corporation",
        "30" => "Viacom",
        "31" => "Nintendo",
        "32" => "Bandai",
        "33" => "Ocean Software/Acclaim Entertainment",
        "34" => "Konami",
        "35" => "HectorSoft",
        "37" => "Taito",
        "38" => "Hudson Soft",
        "39" => "Banpresto",
        "41" => "Ubi Soft1",
        "42" => "Atlus",
        "44" => "Malibu Interactive",
        "46" => "Angel",
        "47" => "Bullet-Proof Software2",
        "49" => "Irem",
        "50" => "Absolute",
        "51" => "Acclaim Entertainment",
        "52" => "Activision",
        "53" => "Sammy USA Corporation",
        "54" => "Konami",
        "55" => "Hi Tech Expressions",
        "56" => "LJN",
        "57" => "Matchbox",
        "58" => "Mattel",
        "59" => "Milton Bradley Company",
        "60" => "Titus Interactive",
        "61" => "Virgin Games Ltd.3",
        "64" => "Lucasfilm Games4",
        "67" => "Ocean Software",
        "69" => "EA (Electronic Arts)",
        "70" => "Infogrames5",
        "71" => "Interplay Entertainment",
        "72" => "Broderbund",
        "73" => "Sculptured Software6",
        "75" => "The Sales Curve Limited7",
        "78" => "THQ",
        "79" => "Accolade8",
        "80" => "Misawa Entertainment",
        "83" => "LOZC G.",
        "86" => "Tokuma Shoten",
        "87" => "Tsukuda Original",
        "91" => "Chunsoft Co.9",
        "92" => "Video System",
        "93" => "Ocean Software/Acclaim Entertainment",
        "95" => "Varie",
        "96" => "Yonezawa10/S'Pal",
        "97" => "Kaneko",
        "99" => "Pack-In-Video",
        "9H" => "Bottom Up",
        "A4" => "Konami (Yu-Gi-Oh!)",
        "BL" => "MTO",
        "DK" => "Kodansha",
        _ => None?,
    };

    Some(String::from(name))
}

fn parse_sgb_flag(rom: &[u8]) -> SgbFlag {
    let flag = rom[SGB_FLAG_ADDR];
    match flag {
        0x03 => SgbFlag::Use,
        _ => SgbFlag::Ignore(flag),
    }
}

fn parse_cartridge_type(rom: &[u8]) -> Result<CartridgeType> {
    let mut ct = CartridgeType {
        mbc: MemoryBlockControllerType::None,
        ram: false,
        battery: false,
        mmm01: false,
        timer: false,
        rumble: false,
        sensor: false,
        pocket_camera: false,
        bandai_tama5: false,
        huc1: false,
        huc3: false,
    };

    use MemoryBlockControllerType as MBC;
    match rom[CARTRIDGE_TYPE_ADDR] {
        0x00 => (),
        0x01 => ct.mbc = MBC::One, // MBC1
        0x02 => {
            ct.mbc = MBC::One;
            ct.ram = true
        } // MBC1+RAM
        0x03 => {
            ct.mbc = MBC::One;
            ct.ram = true;
            ct.battery = true
        } // MBC1+RAM+BATTERY
        0x05 => ct.mbc = MBC::Two, // MBC2
        0x06 => {
            ct.mbc = MBC::Two;
            ct.battery = true
        } // MBC2+BATTERY
        0x08 => ct.ram = true,     // ROM+RAM
        0x09 => {
            ct.ram = true;
            ct.battery = true
        } // ROM+RAM+BATTERY
        0x0B => ct.mmm01 = true,   // MMM01
        0x0C => {
            ct.mmm01 = true;
            ct.ram = true
        } // MMM01+RAM
        0x0D => {
            ct.mmm01 = true;
            ct.ram = true;
            ct.battery = true
        } // MMM01+RAM+BATTERY
        0x0F => {
            ct.mbc = MBC::Three;
            ct.timer = true;
            ct.battery = true
        } // MBC3+TIMER+BATTERY
        0x10 => {
            ct.mbc = MBC::Three;
            ct.timer = true;
            ct.ram = true;
            ct.battery = true
        } // MBC3+TIMER+RAM+BATTERY
        0x11 => ct.mbc = MBC::Three, // MBC3
        0x12 => {
            ct.mbc = MBC::Three;
            ct.ram = true
        } // MBC3+RAM
        0x13 => {
            ct.mbc = MBC::Three;
            ct.ram = true;
            ct.battery = true
        } // MBC3+RAM+BATTERY
        0x19 => ct.mbc = MBC::Five, // MBC5
        0x1A => {
            ct.mbc = MBC::Five;
            ct.ram = true
        } // MBC5+RAM
        0x1B => {
            ct.mbc = MBC::Five;
            ct.ram = true;
            ct.battery = true
        } // MBC5+RAM+BATTERY
        0x1C => {
            ct.mbc = MBC::Five;
            ct.rumble = true
        } // MBC5+RUMBLE
        0x1D => {
            ct.mbc = MBC::Five;
            ct.rumble = true;
            ct.ram = true
        } // MBC5+RUMBLE+RAM
        0x1E => {
            ct.mbc = MBC::Five;
            ct.rumble = true;
            ct.ram = true;
            ct.battery = true
        } // MBC5+RUMBLE+RAM+BATTERY
        0x20 => ct.mbc = MBC::Six, // MBC6
        0x22 => {
            ct.mbc = MBC::Seven;
            ct.sensor = true;
            ct.rumble = true;
            ct.ram = true;
            ct.battery = true
        } // MBC7+SENSOR+RUMBLE+RAM+BATTERY
        0xFC => ct.pocket_camera = true, // POCKET CAMERA
        0xFD => ct.bandai_tama5 = true, // BANDAI TAMA5
        0xFE => ct.huc3 = true,    // HuC3
        0xFF => {
            ct.huc1 = true;
            ct.ram = true;
            ct.battery = true
        } // HuC1+RAM+BATTERY
        x => Err(Error::InvalidCartridgeType(x))?,
    };
    Ok(ct)
}

fn parse_rom_size(rom: &[u8]) -> Result<usize> {
    let size = match rom[ROM_SIZE_ADDR] {
        0x00 => 32 * 1024,
        0x01 => 64 * 1024,
        0x02 => 128 * 1024,
        0x03 => 256 * 1024,
        0x04 => 512 * 1024,
        0x05 => 1 * 1024 * 1024,
        0x06 => 2 * 1024 * 1024,
        0x07 => 4 * 1024 * 1024,
        0x08 => 8 * 1024 * 1024,
        x => Err(Error::UnknownRomSize(x))?,
    };
    Ok(size)
}

fn check_rom_size(rom: &[u8], rom_size: usize) -> Result<()> {
    if rom_size == rom.len() {
        Ok(())
    } else {
        Err(Error::MismatchingRomSizes {
            expected: rom_size,
            actual: rom.len(),
        })
    }
}

fn parse_ram_size(rom: &[u8]) -> Result<usize> {
    let size = match rom[RAM_SIZE_ADDR] {
        0x00 => 0,
        0x02 => 8 * 1024,
        0x03 => 32 * 1024,
        0x04 => 128 * 1024,
        0x05 => 64 * 1024,
        x => Err(Error::UnknownRamSize(x))?,
    };
    Ok(size)
}

fn parse_destincation_code(rom: &[u8]) -> Result<DestinationCode> {
    let code = match rom[DESTINATION_CODE_ADDR] {
        0x00 => DestinationCode::Japan,
        0x01 => DestinationCode::Oversea,
        x => Err(Error::InvalidDestionationCode(x))?,
    };
    Ok(code)
}

fn parse_old_licensee_code(rom: &[u8], new_code: Option<String>) -> Option<String> {
    let code = rom[OLD_LICENSEE_CODE_ADDR];
    if code == 33 {
        return new_code;
    }

    let name = match code {
        0x01 => "Nintendo",
        0x08 => "Capcom",
        0x09 => "HOT-B",
        0x0A => "Jaleco",
        0x0B => "Coconuts Japan",
        0x0C => "Elite Systems",
        0x13 => "EA (Electronic Arts)",
        0x18 => "Hudson Soft",
        0x19 => "ITC Entertainment",
        0x1A => "Yanoman",
        0x1D => "Japan Clary",
        0x1F => "Virgin Games Ltd.3",
        0x24 => "PCM Complete",
        0x25 => "San-X",
        0x28 => "Kemco",
        0x29 => "SETA Corporation",
        0x30 => "Infogrames5",
        0x31 => "Nintendo",
        0x32 => "Bandai",
        0x34 => "Konami",
        0x35 => "HectorSoft",
        0x38 => "Capcom",
        0x39 => "Banpresto",
        0x3C => "Entertainment Interactive (stub)",
        0x3E => "Gremlin",
        0x41 => "Ubi Soft1",
        0x42 => "Atlus",
        0x44 => "Malibu Interactive",
        0x46 => "Angel",
        0x47 => "Spectrum HoloByte",
        0x49 => "Irem",
        0x4A => "Virgin Games Ltd.3",
        0x4D => "Malibu Interactive",
        0x4F => "U.S. Gold",
        0x50 => "Absolute",
        0x51 => "Acclaim Entertainment",
        0x52 => "Activision",
        0x53 => "Sammy USA Corporation",
        0x54 => "GameTek",
        0x55 => "Park Place15",
        0x56 => "LJN",
        0x57 => "Matchbox",
        0x59 => "Milton Bradley Company",
        0x5A => "Mindscape",
        0x5B => "Romstar",
        0x5C => "Naxat Soft16",
        0x5D => "Tradewest",
        0x60 => "Titus Interactive",
        0x61 => "Virgin Games Ltd.3",
        0x67 => "Ocean Software",
        0x69 => "EA (Electronic Arts)",
        0x6E => "Elite Systems",
        0x6F => "Electro Brain",
        0x70 => "Infogrames5",
        0x71 => "Interplay Entertainment",
        0x72 => "Broderbund",
        0x73 => "Sculptured Software6",
        0x75 => "The Sales Curve Limited7",
        0x78 => "THQ",
        0x79 => "Accolade8",
        0x7A => "Triffix Entertainment",
        0x7C => "MicroProse",
        0x7F => "Kemco",
        0x80 => "Misawa Entertainment",
        0x83 => "LOZC G.",
        0x86 => "Tokuma Shoten",
        0x8B => "Bullet-Proof Software2",
        0x8C => "Vic Tokai Corp.17",
        0x8E => "Ape Inc.18",
        0x8F => "I’Max19",
        0x91 => "Chunsoft Co.9",
        0x92 => "Video System",
        0x93 => "Tsubaraya Productions",
        0x95 => "Varie",
        0x96 => "Yonezawa10/S’Pal",
        0x97 => "Kemco",
        0x99 => "Arc",
        0x9A => "Nihon Bussan",
        0x9B => "Tecmo",
        0x9C => "Imagineer",
        0x9D => "Banpresto",
        0x9F => "Nova",
        0xA1 => "Hori Electric",
        0xA2 => "Bandai",
        0xA4 => "Konami",
        0xA6 => "Kawada",
        0xA7 => "Takara",
        0xA9 => "Technos Japan",
        0xAA => "Broderbund",
        0xAC => "Toei Animation",
        0xAD => "Toho",
        0xAF => "Namco",
        0xB0 => "Acclaim Entertainment",
        0xB1 => "ASCII Corporation or Nexsoft",
        0xB2 => "Bandai",
        0xB4 => "Square Enix",
        0xB6 => "HAL Laboratory",
        0xB7 => "SNK",
        0xB9 => "Pony Canyon",
        0xBA => "Culture Brain",
        0xBB => "Sunsoft",
        0xBD => "Sony Imagesoft",
        0xBF => "Sammy Corporation",
        0xC0 => "Taito",
        0xC2 => "Kemco",
        0xC3 => "Square",
        0xC4 => "Tokuma Shoten",
        0xC5 => "Data East",
        0xC6 => "Tonkin House",
        0xC8 => "Koei",
        0xC9 => "UFL",
        0xCA => "Ultra Games",
        0xCB => "VAP, Inc.",
        0xCC => "Use Corporation",
        0xCD => "Meldac",
        0xCE => "Pony Canyon",
        0xCF => "Angel",
        0xD0 => "Taito",
        0xD1 => "SOFEL (Software Engineering Lab)",
        0xD2 => "Quest",
        0xD3 => "Sigma Enterprises",
        0xD4 => "ASK Kodansha Co.",
        0xD6 => "Naxat Soft16",
        0xD7 => "Copya System",
        0xD9 => "Banpresto",
        0xDA => "Tomy",
        0xDB => "LJN",
        0xDD => "Nippon Computer Systems",
        0xDE => "Human Ent.",
        0xDF => "Altron",
        0xE0 => "Jaleco",
        0xE1 => "Towa Chiki",
        0xE2 => "Yutaka # Needs more info",
        0xE3 => "Varie",
        0xE5 => "Epoch",
        0xE7 => "Athena",
        0xE8 => "Asmik Ace Entertainment",
        0xE9 => "Natsume",
        0xEA => "King Records",
        0xEB => "Atlus",
        0xEC => "Epic/Sony Records",
        0xEE => "IGS",
        0xF0 => "A Wave",
        0xF3 => "Extreme Entertainment",
        0xFF => "LJN",
        _ => None?,
    };
    Some(String::from(name))
}

fn parse_rom_version(rom: &[u8]) -> u8 {
    rom[ROM_VERSION_ADDR]
}

fn parse_header_checksum(rom: &[u8]) -> u8 {
    rom[HEADER_CHECKSUM_ADDR]
}

fn check_header_checksum(rom: &[u8], expected: u8) -> Result<()> {
    let mut actual: u8 = 0;
    for addr in 0x0134..=0x014C {
        actual = actual.wrapping_sub(rom[addr]).wrapping_sub(1);
    }

    if expected == actual {
        Ok(())
    } else {
        Err(Error::ViolatedHeaderChecksum { expected, actual })
    }
}

fn parse_global_checksum(rom: &[u8]) -> u16 {
    (rom[GLOBAL_CHECKSUM_ADDR] as u16) << 8 | rom[GLOBAL_CHECKSUM_ADDR + 1] as u16
}

fn check_global_checksum(rom: &[u8], expected: u16) -> Result<()> {
    let actual = rom
        .iter()
        .fold(0, |acc: u16, x| acc.wrapping_add(*x as u16))
        .wrapping_sub(rom[GLOBAL_CHECKSUM_ADDR] as u16)
        .wrapping_sub(rom[GLOBAL_CHECKSUM_ADDR + 1] as u16);

    if expected == actual {
        Ok(())
    } else {
        Err(Error::ViolatedGlobalChecksum { expected, actual })
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn from_file_parses_correctly() {
        let rom = include_bytes!("../../roms/cpu_instrs.gb");
        let result = Header::parse(
            rom,
            &ParseConfig {
                check_nintento_logo: true,
                check_matching_rom_sizes: true,
                check_header_checksum: true,
                check_global_checksum: false,
            },
        );

        let expected = Header {
            title: "CPU_INSTRS".into(),
            cgb_flag: CgbFlag::Monochrom,
            licensee: None,
            sgb_flag: SgbFlag::Ignore(0),
            cartridge_type: CartridgeType {
                mbc: MemoryBlockControllerType::One,
                ram: false,
                battery: false,
                mmm01: false,
                timer: false,
                rumble: false,
                sensor: false,
                pocket_camera: false,
                bandai_tama5: false,
                huc1: false,
                huc3: false,
            },
            rom_size: 65536,
            ram_size: 0,
            destination_code: DestinationCode::Japan,
            rom_version: 0,
            header_checksum: 59,
            global_checksum: 62768,
        };

        assert_eq!(result, Ok(expected));
    }

    #[test]
    fn parse_from_file_fails_global_checksum() {
        let rom = include_bytes!("../../roms/cpu_instrs.gb");
        let result = Header::parse(
            rom,
            &ParseConfig {
                check_global_checksum: true,
                ..Default::default()
            },
        );

        assert_eq!(
            result,
            Err(Error::ViolatedGlobalChecksum {
                expected: 62768,
                actual: 45425
            })
        );
    }
}
