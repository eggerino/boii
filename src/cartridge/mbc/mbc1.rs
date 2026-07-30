use crate::memory::Error;

use super::*;

pub struct MBC1 {
    rom_bank_count: usize,
    reg: Registers,
}

struct Registers {
    rom_bank: usize,
    ram_bank: usize,
    mode: ModeRegister,
}

enum ModeRegister {
    Simple,
    Advanced,
}

fn rom_bank_reg(value: u8) -> usize {
    let mask = 0b0001_1111;
    (value & mask) as usize
}

fn ram_bank_reg(value: u8) -> usize {
    let mask = 0b0000_0011;
    (value & mask) as usize
}

fn mode_reg(value: u8) -> ModeRegister {
    let mask = 0b0000_0001;
    if value & mask == 0 {
        ModeRegister::Simple
    } else {
        ModeRegister::Advanced
    }
}

fn rom_addr(reg: &Registers, address: u16, bank_count: usize) -> Result<usize, Error> {
    let rom_bank_reg = if reg.rom_bank == 0 { 1 } else { reg.rom_bank };
    let selected_bank = rom_bank_reg % bank_count;

    let addr = match (address, &reg.mode) {
        (0x0000..0x4000, ModeRegister::Simple) => address as usize,
        (0x0000..0x4000, ModeRegister::Advanced) => address as usize + reg.ram_bank << 19,
        (0x4000..0x8000, _) => address as usize + selected_bank << 14 + reg.ram_bank << 19,
        _ => Err(Error::SegFault { address })?,
    };

    Ok(addr)
}

fn ram_addr(reg: &Registers, address: u16) -> usize {
    match reg.mode {
        ModeRegister::Simple => address as usize,
        ModeRegister::Advanced => address as usize + (reg.ram_bank << 13),
    }
}

impl MBC1 {
    pub fn new(rom_size: usize) -> Self {
        let rom_bank_count = rom_size / 16;
        Self {
            rom_bank_count,
            reg: Registers {
                rom_bank: 0,
                ram_bank: 0,
                mode: ModeRegister::Simple,
            },
        }
    }
}

impl MemoryBankController for MBC1 {
    fn read_rom(&self, rom: &[u8], address: u16) -> Result<u8, Error> {
        let addr = rom_addr(&self.reg, address, self.rom_bank_count)?;
        read(rom, addr).ok_or(Error::SegFault { address })
    }

    fn write_rom(&mut self, _rom: &mut [u8], address: u16, value: u8) -> Result<(), Error> {
        match address {
            0x0000..0x2000 => (), // Toggle RAM -> no effect to emulate
            0x2000..0x4000 => self.reg.rom_bank = rom_bank_reg(value),
            0x4000..0x6000 => self.reg.ram_bank = ram_bank_reg(value),
            0x6000..0x8000 => self.reg.mode = mode_reg(value),
            _ => Err(Error::SegFault { address })?,
        }
        Ok(())
    }

    fn read_ram(&self, ram: &[u8], address: u16) -> Result<u8, Error> {
        let addr = ram_addr(&self.reg, address);
        read(ram, addr).ok_or(Error::SegFault { address })
    }

    fn write_ram(&mut self, ram: &mut [u8], address: u16, value: u8) -> Result<(), Error> {
        let addr = ram_addr(&self.reg, address);
        write(ram, addr, value).ok_or(Error::SegFault { address })
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn todo_write_tests() {
        assert!(false);
    }
}
