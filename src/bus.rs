use crate::cartridge;
use core::result;
use std::{convert, error, fmt};

pub type Result<T> = result::Result<T, Error>;

#[derive(Debug)]
pub enum Error {
    UnmappedAddress { addr: u16 },
    Cart(cartridge::Error),
}

impl fmt::Display for Error {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "{self:?}")
    }
}

impl error::Error for Error {}

impl convert::From<cartridge::Error> for Error {
    fn from(value: cartridge::Error) -> Self {
        Error::Cart(value)
    }
}

pub struct Bus<'a> {
    pub cart: &'a mut cartridge::Cartridge,
}

impl<'a> Bus<'a> {
    pub fn read(&self, addr: u16) -> Result<u8> {
        match addr {
            CART_ROM_START..CART_ROM_END => Ok(self.cart.read_rom(addr - CART_ROM_START)?),
            CART_RAM_START..CART_RAM_END => Ok(self.cart.read_ram(addr - CART_RAM_START)?),
            _ => Err(Error::UnmappedAddress { addr }),
        }
    }

    pub fn write(&mut self, addr: u16, value: u8) -> Result<()> {
        match addr {
            CART_RAM_START..CART_RAM_END => Ok(self.cart.write_ram(addr - CART_RAM_START, value)?),
            _ => Err(Error::UnmappedAddress { addr }),
        }
    }
}

const CART_ROM_START: u16 = 0x0000;
const CART_ROM_END: u16 = 0x8000;

const CART_RAM_START: u16 = 0xA000;
const CART_RAM_END: u16 = 0xBFFF;
