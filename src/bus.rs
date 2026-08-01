use crate::{
    cartridge::Cartridge,
    memory::{Error, Read, Write},
};

pub struct Bus {
    cartridge: Cartridge,
}

impl Bus {
    pub fn new(cartridge: Cartridge) -> Self {
        Self { cartridge }
    }
}

impl Read for Bus {
    fn read(&self, address: u16) -> Result<u8, Error> {
        match address {
            0x0000..0x7FFF => self.cartridge.rom().read(address),
            0xA000..0xBFFF => self.cartridge.ram().read(address - 0xA000),
            _ => Err(Error::SegFault { address }),
        }
        .map_err(|e| match e {
            Error::SegFault { address: _ } => Error::SegFault { address },
        })
    }
}

impl Write for Bus {
    fn write(&mut self, address: u16, value: u8) -> Result<(), Error> {
        match address {
            0x0000..0x7FFF => self.cartridge.rom_mut().write(address, value),
            0xA000..0xBFFF => self.cartridge.ram_mut().write(address - 0xA000, value),
            _ => Err(Error::SegFault { address }),
        }
        .map_err(|e| match e {
            Error::SegFault { address: _ } => Error::SegFault { address },
        })
    }
}
