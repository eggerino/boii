mod header;
mod mbc;

use crate::{
    cartridge::{
        header::{Header, MBCType},
        mbc::{MBC1, MemoryBankController, NoMBC},
    },
    memory::{self, Read, Write},
};

pub use header::{ParseConfig, ParseError};

#[derive(Debug, PartialEq)]
pub enum LoadError {
    MismatchingRomSizes { expected: usize, actual: usize },
    MismatchingRamSizes { expected: usize, actual: usize },
}

#[derive(Debug, PartialEq)]
pub enum Error {
    ParseError(ParseError),
    LoadError(LoadError),
}

impl From<ParseError> for Error {
    fn from(value: ParseError) -> Self {
        Self::ParseError(value)
    }
}

impl From<LoadError> for Error {
    fn from(value: LoadError) -> Self {
        Self::LoadError(value)
    }
}

pub struct Cartridge {
    header: Header,
    rom: Box<[u8]>,
    ram: Vec<u8>,
    mbc: Box<dyn MemoryBankController>,
}

impl Cartridge {
    pub fn from(
        rom: Box<[u8]>,
        ram: Option<Box<[u8]>>,
        config: &ParseConfig,
    ) -> Result<Self, Error> {
        let header = Header::parse(&rom, config)?;

        // Only battery packed ram can be loaded
        let ram = ram
            .and_then(|r| {
                if header.cartridge_type.battery {
                    Some(r)
                } else {
                    None
                }
            })
            .map(|x| x.into_vec())
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
            MBCType::MBC2 => todo!("Cartridge currently not supported: MCBType = MBC2"),
            MBCType::MMM01 => todo!("Cartridge currently not supported: MCBType = MMM01"),
            MBCType::MBC3 => todo!("Cartridge currently not supported: MCBType = MBC3"),
            MBCType::MBC5 => todo!("Cartridge currently not supported: MCBType = MBC5"),
            MBCType::MBC6 => todo!("Cartridge currently not supported: MCBType = MBC6"),
            MBCType::MBC7 => todo!("Cartridge currently not supported: MCBType = MBC7"),
            MBCType::HuC3 => todo!("Cartridge currently not supported: MCBType = HuC3"),
            MBCType::HuC1 => todo!("Cartridge currently not supported: MCBType = HuC1"),
        };

        if header.cartridge_type.timer {
            todo!("Cartridge currently not supported: timer")
        }
        if header.cartridge_type.rumble {
            todo!("Cartridge currently not supported: rumble")
        }
        if header.cartridge_type.sensor {
            todo!("Cartridge currently not supported: sensor")
        }
        if header.cartridge_type.pocket_camera {
            todo!("Cartridge currently not supported: pocket_camera")
        }
        if header.cartridge_type.bandai_tama5 {
            todo!("Cartridge currently not supported: bandai_tama5")
        }

        Ok(Self {
            header,
            rom,
            ram,
            mbc,
        })
    }

    pub fn title(&self) -> &str {
        &self.header.title
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

    pub fn backing_ram(&self) -> Option<&[u8]> {
        if self.header.cartridge_type.battery {
            Some(&self.ram)
        } else {
            None
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

impl<'a> Write for RomMut<'a> {
    fn write(&mut self, address: u16, value: u8) -> Result<(), memory::Error> {
        self.0.write_rom(&mut self.1, address, value)
    }
}

pub struct Ram<'a>(&'a dyn MemoryBankController, &'a [u8]);

impl<'a> Read for Ram<'a> {
    fn read(&self, address: u16) -> Result<u8, memory::Error> {
        self.0.read_ram(&self.1, address)
    }
}

pub struct RamMut<'a>(&'a mut dyn MemoryBankController, &'a mut [u8]);

impl<'a> Write for RamMut<'a> {
    fn write(&mut self, address: u16, value: u8) -> Result<(), memory::Error> {
        self.0.write_ram(&mut self.1, address, value)
    }
}
