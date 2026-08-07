use crate::{
    cartridge::Cartridge,
    memory::{Error, Read, Write},
    vram::Vram,
};

pub struct Bus {
    interrupt_enable_reg: u8,
    high_ram: [u8; 0x007F],
    vram: Vram,
    cartridge: Cartridge,
}

impl Bus {
    pub fn new(cartridge: Cartridge) -> Self {
        Self {
            interrupt_enable_reg: 0,
            high_ram: [0; 0x007F],
            vram: Vram::new(cartridge.cgb()),
            cartridge,
        }
    }
}

impl Read for Bus {
    fn read(&self, address: u16) -> Result<u8, Error> {
        match address {
            0x0000..0x8000 => self.cartridge.rom().read(address),
            0x8000..0xA000 => self.vram.read(address.saturating_sub(0x8000)),
            0xA000..0xC000 => self.cartridge.ram().read(address.saturating_sub(0xA000)),
            0xC000..0xE000 => todo!("Wram"),
            0xE000..0xFE00 => todo!("Echo ram"),
            0xFE00..0xFEA0 => todo!("Object attribute memory"),
            0xFEA0..0xFF00 => Err(Error::SegFault { address }), // Prohibited usage -> segfault
            0xFF00..0xFF80 => todo!("IO registers"),
            0xFF80..0xFFFF => self.high_ram.read(address.saturating_sub(0xFF80)),
            0xFFFF => Ok(self.interrupt_enable_reg),
        }
        .map_err(|e| match e {
            Error::SegFault { address: _ } => Error::SegFault { address },
        })
    }
}

impl Write for Bus {
    fn write(&mut self, address: u16, value: u8) -> Result<(), Error> {
        match address {
            0x0000..0x8000 => self.cartridge.rom_mut().write(address, value),
            0x8000..0xA000 => self.vram.write(address.saturating_sub(0x8000), value),
            0xA000..0xC000 => self
                .cartridge
                .ram_mut()
                .write(address.saturating_sub(0xA000), value),
            0xC000..0xE000 => todo!("Wram"),
            0xE000..0xFE00 => Err(Error::SegFault { address }), // prohibited usage of echo ram -> segfault
            0xFE00..0xFEA0 => todo!("Object attribute memory"),
            0xFEA0..0xFF00 => Err(Error::SegFault { address }), // Prohibited usage -> segfault
            0xFF00..0xFF80 => todo!("IO registers"),
            0xFF80..0xFFFF => self.high_ram.write(address.saturating_sub(0xFF80), value),
            0xFFFF => {
                self.interrupt_enable_reg = value;
                Ok(())
            }
        }
        .map_err(|e| match e {
            Error::SegFault { address: _ } => Error::SegFault { address },
        })
    }
}
