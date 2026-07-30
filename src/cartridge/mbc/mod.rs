mod mbc1;

use crate::memory::{Error, Read, Write};

pub use mbc1::MBC1;

pub trait MemoryBankController {
    fn read_rom(&self, rom: &[u8], address: u16) -> Result<u8, Error>;
    fn write_rom(&mut self, rom: &mut [u8], address: u16, value: u8) -> Result<(), Error>;

    fn read_ram(&self, ram: &[u8], address: u16) -> Result<u8, Error>;
    fn write_ram(&mut self, ram: &mut [u8], address: u16, value: u8) -> Result<(), Error>;
}

pub struct NoMBC;

impl MemoryBankController for NoMBC {
    fn read_rom(&self, rom: &[u8], address: u16) -> Result<u8, Error> {
        rom.read(address)
    }

    fn write_rom(&mut self, rom: &mut [u8], address: u16, value: u8) -> Result<(), Error> {
        rom.write(address, value)
    }

    fn read_ram(&self, ram: &[u8], address: u16) -> Result<u8, Error> {
        ram.read(address)
    }

    fn write_ram(&mut self, ram: &mut [u8], address: u16, value: u8) -> Result<(), Error> {
        ram.write(address, value)
    }
}

fn read(buffer: &[u8], address: usize) -> Option<u8> {
    buffer.get(address).map(|x| *x)
}

fn write(buffer: &mut [u8], address: usize, value: u8) -> Option<()> {
    buffer.get_mut(address).map(|x| *x = value)
}
