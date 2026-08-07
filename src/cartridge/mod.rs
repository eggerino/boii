mod header;
mod mbc;

use crate::{
    cartridge::{
        header::{CgbFlag, Header, MBCType},
        mbc::{MBC1, MemoryBankController, NoMBC},
    },
    memory::{self, Read},
};
pub use header::ParseConfig;
use std::{fs::File, io::Write};

#[derive(Debug, PartialEq)]
pub enum LoadError {
    SensorCartridge,
    CameraCartridge,
    BandaiTama5Cartridge,
    MismatchingRomSizes { expected: usize, actual: usize },
    MismatchingRamSizes { expected: usize, actual: usize },
}

impl core::fmt::Display for LoadError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            LoadError::SensorCartridge => write!(
                f,
                "The cartridge has a sensor. This is not supported in emulation."
            ),
            LoadError::CameraCartridge => write!(
                f,
                "The cartridge has a camera. This is not supported in emulation."
            ),
            LoadError::BandaiTama5Cartridge => write!(
                f,
                "The cartridge has a tama5. This is not supported in emulation."
            ),
            LoadError::MismatchingRomSizes { expected, actual } => write!(
                f,
                "The rom has a size of {} but the header specifies a size of {}.",
                actual, expected
            ),
            LoadError::MismatchingRamSizes { expected, actual } => write!(
                f,
                "The ram has a size of {} but the header specifies a size of {}.",
                actual, expected
            ),
        }
    }
}

impl core::error::Error for LoadError {}

#[derive(Debug, PartialEq)]
pub enum Error {
    Header(header::Error),
    LoadError(LoadError),
}

impl core::fmt::Display for Error {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Error::Header(e) => e.fmt(f),
            Error::LoadError(e) => e.fmt(f),
        }
    }
}

impl core::error::Error for Error {}

impl From<header::Error> for Error {
    fn from(value: header::Error) -> Self {
        Self::Header(value)
    }
}

impl From<LoadError> for Error {
    fn from(value: LoadError) -> Self {
        Self::LoadError(value)
    }
}

pub struct Cartridge {
    header: Header,
    rom: Vec<u8>,
    ram: Vec<u8>,
    mbc: Box<dyn MemoryBankController>,
    ram_file: Option<String>,
}

impl Cartridge {
    pub fn from(
        rom: Vec<u8>,
        ram: Option<Vec<u8>>,
        config: &ParseConfig,
        ram_file: Option<String>,
    ) -> Result<Self, Error> {
        let header = Header::parse(&rom, config)?;

        if header.cartridge_type.sensor {
            Err(LoadError::SensorCartridge)?;
        }
        if header.cartridge_type.pocket_camera {
            Err(LoadError::CameraCartridge)?;
        }
        if header.cartridge_type.bandai_tama5 {
            Err(LoadError::BandaiTama5Cartridge)?;
        }

        // Only battery packed ram can be loaded
        let ram = ram
            .and_then(|r| {
                if header.cartridge_type.battery {
                    Some(r)
                } else {
                    None
                }
            })
            .unwrap_or_else(|| vec![0; header.ram_size]);

        if rom.len() != header.rom_size {
            Err(LoadError::MismatchingRomSizes {
                expected: header.rom_size,
                actual: rom.len(),
            })?;
        }

        if ram.len() != header.ram_size {
            Err(LoadError::MismatchingRamSizes {
                expected: header.ram_size,
                actual: ram.len(),
            })?;
        }

        let mbc: Box<dyn MemoryBankController> = match header.cartridge_type.mbc {
            MBCType::None => Box::new(NoMBC),
            MBCType::MBC1 => Box::new(MBC1::new(rom.len())),
            MBCType::MBC2 => todo!("Implement cartridge mbc \"MBC2\""),
            MBCType::MMM01 => todo!("Implement cartridge mbc \"MMM01\""),
            MBCType::MBC3 => todo!("Implement cartridge mbc \"MBC3\""),
            MBCType::MBC5 => todo!("Implement cartridge mbc \"MBC5\""),
            MBCType::MBC6 => todo!("Implement cartridge mbc \"MBC6\""),
            MBCType::MBC7 => todo!("Implement cartridge mbc \"MBC7\""),
            MBCType::HuC3 => todo!("Implement cartridge mbc \"HuC3\""),
            MBCType::HuC1 => todo!("Implement cartridge mbc \"HuC1\""),
        };

        if header.cartridge_type.timer {
            todo!("Implement cartridge timer")
        }

        Ok(Self {
            header,
            rom,
            ram,
            mbc,
            ram_file,
        })
    }

    pub fn title(&self) -> &str {
        &self.header.title
    }

    pub fn cgb(&self) -> bool {
        self.header.cgb_flag == CgbFlag::Color
    }

    pub fn rom(&self) -> Rom<'_> {
        Rom(self.mbc.as_ref(), &self.rom)
    }

    pub fn rom_mut(&mut self) -> RomMut<'_> {
        RomMut(self.mbc.as_mut(), &mut self.rom)
    }

    pub fn ram(&self) -> Ram<'_> {
        Ram(self.mbc.as_ref(), &self.ram)
    }

    pub fn ram_mut(&mut self) -> RamMut<'_> {
        RamMut(self.mbc.as_mut(), &mut self.ram)
    }
}

impl Drop for Cartridge {
    fn drop(&mut self) {
        // Only safe battery packed ram when the cartridge actually has a battery
        if self.header.cartridge_type.battery
            && let Some(path) = self.ram_file.as_ref()
        {
            // Open a new file or overwrite an existing one
            let result = File::create(path).and_then(|mut f| f.write_all(&self.ram));

            if let Err(e) = result {
                eprintln!("Could not save the ram. {}", e);
            }
        }
    }
}

pub struct Rom<'a>(&'a dyn MemoryBankController, &'a [u8]);

impl<'a> Read for Rom<'a> {
    fn read(&self, address: u16) -> Result<u8, memory::Error> {
        self.0.read_rom(self.1, address)
    }
}

pub struct RomMut<'a>(&'a mut dyn MemoryBankController, &'a mut [u8]);

impl<'a> memory::Write for RomMut<'a> {
    fn write(&mut self, address: u16, value: u8) -> Result<(), memory::Error> {
        self.0.write_rom(self.1, address, value)
    }
}

pub struct Ram<'a>(&'a dyn MemoryBankController, &'a [u8]);

impl<'a> Read for Ram<'a> {
    fn read(&self, address: u16) -> Result<u8, memory::Error> {
        self.0.read_ram(self.1, address)
    }
}

pub struct RamMut<'a>(&'a mut dyn MemoryBankController, &'a mut [u8]);

impl<'a> memory::Write for RamMut<'a> {
    fn write(&mut self, address: u16, value: u8) -> Result<(), memory::Error> {
        self.0.write_ram(self.1, address, value)
    }
}
