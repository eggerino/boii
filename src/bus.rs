use std::{cell::RefCell, rc::Rc};

use crate::{
    cartridge::Cartridge,
    memory::{Error, Read, Result, Write},
};

pub struct Bus {
    cartridge: Rc<RefCell<Cartridge>>,
}

impl Bus {
    pub fn new(cartridge: Rc<RefCell<Cartridge>>) -> Self {
        Self { cartridge }
    }
}

impl Read for Bus {
    fn read(&self, address: u16) -> Result<u8> {
        let result = match address {
            0x0000..0x7FFF => self.cartridge.borrow().rom().read(address),
            0xA000..0xBFFF => self
                .cartridge
                .borrow()
                .ram()
                .map(|x| x.read(address))
                .ok_or(Error::SegFault { address })
                .flatten(),
            _ => Err(Error::SegFault { address }),
        };

        result.map_err(|e| match e {
            Error::SegFault { address: _ } => Error::SegFault { address },
        })
    }
}

impl Write for Bus {
    fn write(&mut self, address: u16, value: u8) -> Result<()> {
        let result = match address {
            0xA000..0xBFFF => self
                .cartridge
                .borrow_mut()
                .ram_mut()
                .map(|x| x.write(address, value))
                .ok_or(Error::SegFault { address })
                .flatten(),
            _ => Err(Error::SegFault { address }),
        };

        result.map_err(|e| match e {
            Error::SegFault { address: _ } => Error::SegFault { address },
        })
    }
}
