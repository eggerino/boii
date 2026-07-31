mod mbc1;

use crate::memory::{Error, Read, Write};

pub use mbc1::MBC1;

pub trait MemoryBankController {
    fn read_rom(&self, backing_rom: &[u8], address: u16) -> Result<u8, Error>;
    fn write_rom(&mut self, backing_rom: &mut [u8], address: u16, value: u8) -> Result<(), Error>;

    fn read_ram(&self, backing_ram: &[u8], address: u16) -> Result<u8, Error>;
    fn write_ram(&mut self, backing_ram: &mut [u8], address: u16, value: u8) -> Result<(), Error>;
}

pub struct NoMBC;

impl MemoryBankController for NoMBC {
    fn read_rom(&self, backing_rom: &[u8], address: u16) -> Result<u8, Error> {
        backing_rom.read(address)
    }

    fn write_rom(&mut self, backing_rom: &mut [u8], address: u16, value: u8) -> Result<(), Error> {
        backing_rom.write(address, value)
    }

    fn read_ram(&self, backing_ram: &[u8], address: u16) -> Result<u8, Error> {
        backing_ram.read(address)
    }

    fn write_ram(&mut self, backing_ram: &mut [u8], address: u16, value: u8) -> Result<(), Error> {
        backing_ram.write(address, value)
    }
}
