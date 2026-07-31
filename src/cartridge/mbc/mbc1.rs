use super::MemoryBankController;
use crate::memory::Error;

const ROM_BANK_SIZE: usize = 16 * 1024;
const ROM_BANK_REG_MASK: u8 = 0b0001_1111;
const RAM_BANK_REG_MASK: u8 = 0b0000_0011;
const MODE_REG_MASK: u8 = 0b0000_0001;
const ROM_ADDR_MASK: u16 = 0b0011_1111_1111_1111;
const RAM_ADDR_MASK: u16 = 0b0001_1111_1111_1111;

pub struct MBC1 {
    rom_bank_count: u8,
    reg: Registers,
    offsets: Offsets,
}

struct Offsets {
    lower_rom: usize,
    upper_rom: usize,
    ram: usize,
}

struct Registers {
    rom_bank: u8,
    ram_bank: u8,
    mode: u8,
}

fn offsets_with(reg: &Registers, rom_bank_count: u8) -> Offsets {
    let rom_bank = ((match reg.rom_bank & ROM_BANK_REG_MASK {
        0 => 1,
        x => x,
    } % rom_bank_count)
        & ROM_BANK_REG_MASK) as usize;

    let ram_bank = (reg.ram_bank & RAM_BANK_REG_MASK) as usize;
    let mode = (reg.mode & MODE_REG_MASK) == 1;

    let lower_rom = if mode { ram_bank << 19 } else { 0 };
    let upper_rom = (rom_bank << 14) | (ram_bank << 19);
    let ram = if mode { ram_bank << 13 } else { 0 };

    Offsets {
        lower_rom,
        upper_rom,
        ram,
    }
}

fn backing_rom_addr(address: u16, offsets: &Offsets) -> usize {
    let base_addr = (address & ROM_ADDR_MASK) as usize;
    match address {
        ..0x4000 => offsets.lower_rom | base_addr,
        0x4000.. => offsets.upper_rom | base_addr,
    }
}

fn backing_ram_addr(address: u16, offsets: &Offsets) -> usize {
    let base_addr = (address & RAM_ADDR_MASK) as usize;
    offsets.ram | base_addr
}

impl MBC1 {
    pub fn new(rom_size: usize) -> Self {
        let rom_bank_count = (rom_size / ROM_BANK_SIZE) as u8;
        let reg = Registers {
            rom_bank: 0,
            ram_bank: 0,
            mode: 0,
        };
        let offsets = offsets_with(&reg, rom_bank_count);

        Self {
            rom_bank_count,
            reg,
            offsets,
        }
    }
}

impl MemoryBankController for MBC1 {
    fn read_rom(&self, backing_rom: &[u8], address: u16) -> Result<u8, Error> {
        backing_rom
            .get(backing_rom_addr(address, &self.offsets))
            .map(|x| *x)
            .ok_or(Error::SegFault { address })
    }

    fn write_rom(&mut self, _backing_rom: &mut [u8], address: u16, value: u8) -> Result<(), Error> {
        match address {
            0x0000..0x2000 => (), // Toggle RAM -> no effect to emulate
            0x2000..0x4000 => self.reg.rom_bank = value,
            0x4000..0x6000 => self.reg.ram_bank = value,
            0x6000..0x8000 => self.reg.mode = value,
            _ => Err(Error::SegFault { address })?,
        };
        self.offsets = offsets_with(&self.reg, self.rom_bank_count);
        Ok(())
    }

    fn read_ram(&self, backing_ram: &[u8], address: u16) -> Result<u8, Error> {
        backing_ram
            .get(backing_ram_addr(address, &self.offsets))
            .map(|x| *x)
            .ok_or(Error::SegFault { address })
    }

    fn write_ram(&mut self, backing_ram: &mut [u8], address: u16, value: u8) -> Result<(), Error> {
        backing_ram
            .get_mut(backing_ram_addr(address, &self.offsets))
            .map(|x| *x = value)
            .ok_or(Error::SegFault { address })
    }
}

#[cfg(test)]
mod tests {
    use super::*;

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
    fn read_rom_bank_zero_mirror_bug() {
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
