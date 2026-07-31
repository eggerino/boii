use super::MemoryBankController;
use crate::memory::Error;

pub struct MBC1 {
    rom_bank_count: usize,
    reg: Registers,
}

struct Registers {
    rom_bank: usize,
    ram_bank: usize,
    mode: Mode,
}

#[derive(Debug, PartialEq)]
enum Mode {
    Simple,
    Advanced,
}

fn extract_rom_bank_reg(value: u8) -> usize {
    let mask = 0b0001_1111;
    (value & mask) as usize
}

fn extract_ram_bank_reg(value: u8) -> usize {
    let mask = 0b0000_0011;
    (value & mask) as usize
}

fn extract_mode_reg(value: u8) -> Mode {
    let mask = 0b0000_0001;
    if value & mask == 0 {
        Mode::Simple
    } else {
        Mode::Advanced
    }
}

fn backing_rom_addr(mbc: &MBC1, address: u16) -> Option<usize> {
    let mask = 0b0011_1111_1111_1111;
    let base_addr = (address & mask) as usize;

    let mask = 0b0001_1111;
    let rom_bank = ((if mbc.reg.rom_bank == 0 {
        1
    } else {
        mbc.reg.rom_bank
    } % mbc.rom_bank_count)
        & mask) as usize;

    let ram_bank = mbc.reg.ram_bank;

    match (address, &mbc.reg.mode) {
        (0x0000..0x4000, Mode::Simple) => Some(base_addr),
        (0x0000..0x4000, Mode::Advanced) => Some(base_addr + (ram_bank << 19)), // 2-bit register for ram bank gets reinterpreted in advanced mode
        (0x4000..0x8000, _) => Some(base_addr + (rom_bank << 14) + (ram_bank << 19)),
        _ => None,
    }
}

fn backing_ram_addr(mbc: &MBC1, address: u16) -> usize {
    match mbc.reg.mode {
        Mode::Simple => address as usize,
        Mode::Advanced => address as usize + (mbc.reg.ram_bank << 13),
    }
}

impl MBC1 {
    pub fn new(rom_size: usize) -> Self {
        let rom_bank_count = rom_size / (16 * 1024); // Each rom bank is 16 KiB large

        Self {
            rom_bank_count,
            reg: Registers {
                rom_bank: 0,
                ram_bank: 0,
                mode: Mode::Simple,
            },
        }
    }
}

impl MemoryBankController for MBC1 {
    fn read_rom(&self, backing_rom: &[u8], address: u16) -> Result<u8, Error> {
        backing_rom_addr(self, address)
            .map(|x| backing_rom.get(x))
            .flatten()
            .map(|x| *x)
            .ok_or(Error::SegFault { address })
    }

    fn write_rom(&mut self, _backing_rom: &mut [u8], address: u16, value: u8) -> Result<(), Error> {
        match address {
            0x0000..0x2000 => (), // Toggle RAM -> no effect to emulate
            0x2000..0x4000 => self.reg.rom_bank = extract_rom_bank_reg(value),
            0x4000..0x6000 => self.reg.ram_bank = extract_ram_bank_reg(value),
            0x6000..0x8000 => self.reg.mode = extract_mode_reg(value),
            _ => Err(Error::SegFault { address })?,
        }
        Ok(())
    }

    fn read_ram(&self, backing_ram: &[u8], address: u16) -> Result<u8, Error> {
        backing_ram
            .get(backing_ram_addr(self, address))
            .map(|x| *x)
            .ok_or(Error::SegFault { address })
    }

    fn write_ram(&mut self, backing_ram: &mut [u8], address: u16, value: u8) -> Result<(), Error> {
        backing_ram
            .get_mut(backing_ram_addr(self, address))
            .map(|x| *x = value)
            .ok_or(Error::SegFault { address })
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn write_rom_sets_registers() {
        let mut mbc1 = MBC1::new(16 * 1024);
        let mut rom = [0; 10];

        mbc1.write_rom(&mut rom, 0x2000, 255).unwrap();
        mbc1.write_rom(&mut rom, 0x4000, 255).unwrap();
        mbc1.write_rom(&mut rom, 0x6000, 255).unwrap();

        assert_eq!(mbc1.reg.rom_bank, 0b0001_1111);
        assert_eq!(mbc1.reg.ram_bank, 0b0000_0011);
        assert_eq!(mbc1.reg.mode, Mode::Advanced);
    }

    #[test]
    fn read_rom_different_banks() {
        let rom_size = 0b1_0000_0000_0000_0000_0000;

        let mut rom = vec![0; rom_size];
        let mut mbc1 = MBC1::new(rom.len());

        rom[0x1234] = 1;
        rom[0x4567] = 2;
        rom[0b1000_0000_0000_0000_0000 + 0x1234] = 3;
        rom[0b1000_1100_0000_0000_0000 + 0x0567] = 4;

        assert_eq!(mbc1.read_rom(&rom, 0x1234), Ok(1));
        assert_eq!(mbc1.read_rom(&rom, 0x4567), Ok(2));

        mbc1.write_rom(&mut rom, 0x2000, 0b0011).unwrap();
        mbc1.write_rom(&mut rom, 0x4000, 0b0001).unwrap();
        mbc1.write_rom(&mut rom, 0x6000, 1).unwrap();

        assert_eq!(mbc1.read_rom(&rom, 0x1234), Ok(3));
        assert_eq!(mbc1.read_rom(&rom, 0x4567), Ok(4));
    }

    #[test]
    fn read_rom_bank0_mirror_bug() {
        let mut rom = vec![0; 64 * 1024];
        let mut mbc1 = MBC1::new(rom.len());

        rom[0] = 1;
        rom[2] = 2;

        mbc1.write_rom(&mut rom, 0x2000, 0b1000).unwrap();

        assert_eq!(mbc1.read_rom(&rom, 0x0000), Ok(1));
        assert_eq!(mbc1.read_rom(&rom, 0x0002), Ok(2));
        assert_eq!(mbc1.read_rom(&rom, 0x4000), Ok(1)); // mirrored
        assert_eq!(mbc1.read_rom(&rom, 0x4002), Ok(2)); // mirrored
    }

    #[test]
    fn read_ram_different_banks() {
        let mut ram = vec![0; 0x6001];
        let mut mbc1 = MBC1::new(32 * 1024);

        ram[0x0000] = 1;
        ram[0x2000] = 2;
        ram[0x4000] = 3;
        ram[0x6000] = 4;

        assert_eq!(mbc1.read_ram(&ram, 0x0000), Ok(1));

        mbc1.write_rom(&mut ram, 0x6000, 1).unwrap();
        mbc1.write_rom(&mut ram, 0x4000, 0b0001).unwrap();
        assert_eq!(mbc1.read_ram(&ram, 0x0000), Ok(2));

        mbc1.write_rom(&mut ram, 0x4000, 0b0010).unwrap();
        assert_eq!(mbc1.read_ram(&ram, 0x0000), Ok(3));

        mbc1.write_rom(&mut ram, 0x4000, 0b0011).unwrap();
        assert_eq!(mbc1.read_ram(&ram, 0x0000), Ok(4));
    }
}
