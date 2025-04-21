use core::result;
use std::{convert, error, fmt, fs, io, string};

pub type Result<T> = result::Result<T, Error>;

#[derive(Debug)]
pub enum Error {
    // Validation
    InconsistentRomSize { actual: usize, header: usize },
    MissingNintentoLogo,
    InvalidHeaderChecksum { actual: u8, header: u8 },
    InvalidGlobalChecksum { actual: u16, header: u16 },

    // Access violation
    ReadAccessViolation { addr: u16 },
    WriteAccessViolation { addr: u16 },

    // Parsing
    NoHeader,
    UnknownRomSizeValue { value: u8 },
    UnknownRamSizeValue { value: u8 },

    // External
    NoAsciiString(string::FromUtf8Error),
    Io(io::Error),
}

impl fmt::Display for Error {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "{self:?}")
    }
}

impl error::Error for Error {}

impl convert::From<io::Error> for Error {
    fn from(value: io::Error) -> Self {
        Error::Io(value)
    }
}

impl convert::From<std::string::FromUtf8Error> for Error {
    fn from(value: string::FromUtf8Error) -> Self {
        Error::NoAsciiString(value)
    }
}

pub struct ValidationSettings {
    pub check_rom_size: bool,
    pub check_nintento_logo: bool,
    pub check_header_checksum: bool,
    pub check_global_checksum: bool,
}

impl ValidationSettings {
    pub fn default() -> Self {
        Self {
            check_rom_size: true,
            check_nintento_logo: true,
            check_header_checksum: true,
            check_global_checksum: false,
        }
    }
}

#[derive(Debug)]
pub struct Cartridge {
    rom: Vec<u8>,
    ram: Vec<u8>,
    header: Header,
}

impl Cartridge {
    // Factory
    pub fn from_rom_data(rom: Vec<u8>, settings: &ValidationSettings) -> Result<Self> {
        let header = Header::from_rom(&rom)?;
        Self::validate_rom(&rom, &header, settings)?;
        Ok(Self {
            rom,
            ram: vec![0; header.ram_size],
            header,
        })
    }

    pub fn from_rom_file(path: &str, settings: &ValidationSettings) -> Result<Self> {
        let rom = fs::read(path)?;
        Self::from_rom_data(rom, settings)
    }

    fn validate_rom(rom: &[u8], header: &Header, settings: &ValidationSettings) -> Result<()> {
        if settings.check_rom_size && rom.len() != header.rom_size {
            return Err(Error::InconsistentRomSize {
                actual: rom.len(),
                header: header.rom_size,
            });
        }

        if settings.check_nintento_logo
            && rom[NINTENDO_LOGO_PTR..NINTENDO_LOGO_PTR + NINTENDO_LOGO_SIZE] != NINTENDO_LOGO
        {
            return Err(Error::MissingNintentoLogo);
        }

        let header_checksum = Self::compute_header_checksum(rom);
        if settings.check_header_checksum && header_checksum != header.header_checksum {
            return Err(Error::InvalidHeaderChecksum {
                actual: header_checksum,
                header: header.header_checksum,
            });
        }

        let global_checksum = Self::compute_global_checksum(rom);
        if settings.check_global_checksum && global_checksum != header.global_checksum {
            return Err(Error::InvalidGlobalChecksum {
                actual: global_checksum,
                header: header.global_checksum,
            });
        }

        Ok(())
    }

    fn compute_header_checksum(rom: &[u8]) -> u8 {
        let mut checksum: u8 = 0;
        for addr in 0x0134..=0x014C {
            checksum = checksum.wrapping_sub(rom[addr]).wrapping_sub(1);
        }
        checksum
    }

    fn compute_global_checksum(rom: &[u8]) -> u16 {
        let mut checksum: u16 = 0;
        for byte in rom {
            checksum = checksum.wrapping_add(*byte as u16);
        }
        checksum
            .wrapping_sub(rom[GLOBAL_CHECKSUM_PTR] as u16)
            .wrapping_sub(rom[GLOBAL_CHECKSUM_PTR + 1] as u16)
    }

    // Info
    pub fn get_title(&self) -> &str {
        &self.header.title
    }

    // Memory
    pub fn read_rom(&self, addr: u16) -> Result<u8> {
        Self::read_from(&self.rom, addr)
    }

    pub fn read_ram(&self, addr: u16) -> Result<u8> {
        Self::read_from(&self.ram, addr)
    }

    fn read_from(buf: &[u8], addr: u16) -> Result<u8> {
        if (addr as usize) < buf.len() {
            Ok(buf[addr as usize])
        } else {
            Err(Error::ReadAccessViolation { addr })
        }
    }

    pub fn write_ram(&mut self, addr: u16, value: u8) -> Result<()> {
        Self::write_to(&mut self.ram, addr, value)
    }

    fn write_to(buf: &mut [u8], addr: u16, value: u8) -> Result<()> {
        if (addr as usize) < buf.len() {
            buf[addr as usize] = value;
            Ok(())
        } else {
            Err(Error::WriteAccessViolation { addr })
        }
    }
}

#[allow(dead_code)]
#[derive(Debug)]
struct Header {
    title: String,
    new_licensee_code: String,
    sgb_flag: u8,
    cartridge_type: u8,
    rom_size: usize,
    ram_size: usize,
    destination_code: u8,
    old_licensee_code: u8,
    rom_version: u8,
    header_checksum: u8,
    global_checksum: u16,
}

impl Header {
    fn from_rom(rom: &[u8]) -> Result<Self> {
        if rom.len() < HEADER_SIZE {
            return Err(Error::NoHeader);
        }

        Ok(Self {
            title: Self::from_ascii(&rom[TITLE_PTR..TITLE_PTR + TITLE_SIZE])?,
            new_licensee_code: Self::from_ascii(
                &rom[NEW_LICENSEE_CODE_PTR..NEW_LICENSEE_CODE_PTR + NEW_LICENSEE_CODE_SIZE],
            )?,
            sgb_flag: rom[SGB_FLAG_PTR],
            cartridge_type: rom[CARTRIDGE_TYPE_PTR],
            rom_size: Self::get_rom_size(rom[ROM_SIZE_PTR])?,
            ram_size: Self::get_ram_size(rom[RAM_SIZE_PTR])?,
            destination_code: rom[DESTINATION_CODE_PTR],
            old_licensee_code: rom[OLD_LICENSEE_CODE_PTR],
            rom_version: rom[ROM_VERSION_PTR],
            header_checksum: rom[HEADER_CHECKSUM_PTR],
            global_checksum: rom[GLOBAL_CHECKSUM_PTR] as u16 * 256
                + rom[GLOBAL_CHECKSUM_PTR + 1] as u16,
        })
    }

    fn from_ascii(buf: &[u8]) -> Result<String> {
        let chars = buf
            .iter()
            .map(|x| *x)
            .take_while(|x| *x != 0)
            .collect::<Vec<_>>();

        String::from_utf8(chars).map_err(Error::NoAsciiString)
    }

    fn get_rom_size(value: u8) -> Result<usize> {
        match value {
            0..=8 => Ok(32 * 1024 * (1 << value)),
            0x52 => Ok(1_153_434), // 1.1 MB
            0x53 => Ok(1_258_292), // 1.2 MB
            0x54 => Ok(1_572_864), // 1.5 MB
            _ => Err(Error::UnknownRomSizeValue { value }),
        }
    }

    fn get_ram_size(value: u8) -> Result<usize> {
        match value {
            0 => Ok(0),
            2 => Ok(8 * 1024),
            3 => Ok(32 * 1024),
            4 => Ok(128 * 1024),
            5 => Ok(64 * 1024),
            _ => Err(Error::UnknownRamSizeValue { value }),
        }
    }
}

const NINTENDO_LOGO_PTR: usize = 0x0104;
const TITLE_PTR: usize = 0x0134;
const NEW_LICENSEE_CODE_PTR: usize = 0x0144;
const SGB_FLAG_PTR: usize = 0x0146;
const CARTRIDGE_TYPE_PTR: usize = 0x0147;
const ROM_SIZE_PTR: usize = 0x0148;
const RAM_SIZE_PTR: usize = 0x0149;
const DESTINATION_CODE_PTR: usize = 0x014A;
const OLD_LICENSEE_CODE_PTR: usize = 0x014B;
const ROM_VERSION_PTR: usize = 0x014C;
const HEADER_CHECKSUM_PTR: usize = 0x014D;
const GLOBAL_CHECKSUM_PTR: usize = 0x014E;

const HEADER_SIZE: usize = 0x014F;
const NINTENDO_LOGO_SIZE: usize = 0x30;
const TITLE_SIZE: usize = 0x10;
const NEW_LICENSEE_CODE_SIZE: usize = 0x02;

const NINTENDO_LOGO: [u8; NINTENDO_LOGO_SIZE] = [
    0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D,
    0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99,
    0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E,
];

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn create_from_demo_loads_correct() {
        let cart = Cartridge::from_rom_file("./roms/cpu_instrs.gb", &ValidationSettings::default());

        assert!(cart.is_ok());
    }

    #[test]
    fn create_from_demo_with_global_checksum_fails() {
        let cart = Cartridge::from_rom_file(
            "./roms/cpu_instrs.gb",
            &ValidationSettings {
                check_rom_size: true,
                check_nintento_logo: true,
                check_header_checksum: true,
                check_global_checksum: true,
            },
        );

        assert!(cart.is_err());
        assert!(matches!(
            cart.unwrap_err(),
            Error::InvalidGlobalChecksum { .. }
        ));
    }
}
