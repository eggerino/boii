use crate::cartridge::{self, Cartridge};

pub type Result<T> = core::result::Result<T, Error>;
pub enum Error {
    UnmappedAddress { addr: u16 },
    Cart(cartridge::Error),
}

pub struct Bus<'a> {
    pub cart: &'a mut Cartridge,
}

impl<'a> Bus<'a> {
    pub fn read(&self, addr: u16) -> Result<u8> {
        match addr {
            CART_ROM_START..CART_ROM_END => self
                .cart
                .read_rom(addr - CART_RAM_START)
                .map_err(Error::Cart),
            CART_RAM_START..CART_RAM_END => self
                .cart
                .read_ram(addr - CART_RAM_START)
                .map_err(Error::Cart),
            _ => Err(Error::UnmappedAddress { addr }),
        }
    }

    pub fn write(&mut self, addr: u16, value: u8) -> Result<()> {
        match addr {
            CART_RAM_START..CART_RAM_END => self
                .cart
                .write_ram(addr - CART_RAM_START, value)
                .map_err(Error::Cart),
            _ => Err(Error::UnmappedAddress { addr }),
        }
    }
}

const CART_ROM_START: u16 = 0x0000;
const CART_ROM_END: u16 = 0x8000;

const CART_RAM_START: u16 = 0xA000;
const CART_RAM_END: u16 = 0xBFFF;
