use crate::{
    bits::Bits,
    memory::{Error, Read, Write},
};

pub struct Vram {
    offset: u16,
    buf: Vec<u8>,
}

impl Read for Vram {
    fn read(&self, address: u16) -> Result<u8, Error> {
        self.buf.read(address.wrapping_sub(self.offset))
    }
}

impl Write for Vram {
    fn write(&mut self, address: u16, value: u8) -> Result<(), Error> {
        self.buf.write(address.wrapping_sub(self.offset), value)
    }
}

impl Vram {
    pub fn new(cgb: bool) -> Self {
        let size = if cgb { 0x4000 } else { 0x2000 };

        Self {
            offset: 0,
            buf: vec![0; size],
        }
    }

    pub fn use_bank0(&mut self) {
        self.offset = 0;
    }

    pub fn use_bank1(&mut self) {
        self.offset = 0x2000;
    }

    pub fn lower_tile_bank0(&self, idx: u8) -> Option<Tile<'_>> {
        let addr = 16_usize.wrapping_mul(idx as usize);
        self.tile(addr)
    }

    pub fn higher_tile_bank0(&self, idx: u8) -> Option<Tile<'_>> {
        let addr = 16_i32.wrapping_mul((idx as i8) as i32).wrapping_add(0x1000) as usize;
        self.tile(addr)
    }

    pub fn lower_tile_bank1(&self, idx: u8) -> Option<Tile<'_>> {
        let addr = 16_usize.wrapping_mul(idx as usize).wrapping_add(0x2000);
        self.tile(addr)
    }

    pub fn higher_tile_bank1(&self, idx: u8) -> Option<Tile<'_>> {
        let addr = 16_i32.wrapping_mul((idx as i8) as i32).wrapping_add(0x3000) as usize;
        self.tile(addr)
    }

    fn tile(&self, addr: usize) -> Option<Tile<'_>> {
        self.buf.get(addr..addr.saturating_add(16)).map(Tile)
    }

    pub fn lower_tile_map(&self) -> Option<TileMap<'_>> {
        self.tile_map(0x1800)
    }

    pub fn higher_tile_map(&self) -> Option<TileMap<'_>> {
        self.tile_map(0x1C00)
    }

    fn tile_map(&self, addr: usize) -> Option<TileMap<'_>> {
        self.buf.get(addr..addr.saturating_add(0x0400)).map(TileMap)
    }
}

pub struct Tile<'a>(&'a [u8]);

impl<'a> Tile<'a> {
    pub fn pixel(&self, row: u8, col: u8) -> Option<ColorIndex> {
        let lsb_idx = 2_usize.wrapping_mul(row as usize);
        let msb_idx = 2_usize.wrapping_mul(row as usize).wrapping_add(1);

        let lsb = self.0.get(lsb_idx)?.bit(col as i32);
        let msb = self.0.get(msb_idx)?.bit(col as i32);

        Some(ColorIndex::from(msb, lsb))
    }
}

#[derive(PartialEq, Debug)]
pub enum ColorIndex {
    Zero,
    One,
    Two,
    Three,
}

impl ColorIndex {
    fn from(msb: bool, lsb: bool) -> Self {
        match (msb, lsb) {
            (false, false) => Self::Zero,
            (false, true) => Self::One,
            (true, false) => Self::Two,
            (true, true) => Self::Three,
        }
    }
}

pub struct TileMap<'a>(&'a [u8]);

impl<'a> TileMap<'a> {
    pub fn tile_idx(&self, row: u8, col: u8) -> Option<u8> {
        if row < 32 && col < 32 {
            let i = 32_u8.wrapping_mul(row).wrapping_add(col) as usize;
            self.0.get(i).copied()
        } else {
            None
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn tile() {
        let mut vram = Vram::new(true);
        // block, bank, tile, row, lsb/mdb, col
        vram.write(0x0000 + 0x8000 - 0x8000 + 16 + 04 + 0, 0b0000_1000)
            .unwrap();
        vram.write(0x0000 + 0x9000 - 0x8000 + 32 + 06 + 1, 0b0001_0000)
            .unwrap();
        vram.write(0x2000 + 0x8800 - 0x8000 + 48 + 08 + 0, 0b0010_0000)
            .unwrap();
        vram.write(0x2000 + 0x8800 - 0x8000 + 48 + 08 + 1, 0b0010_0000)
            .unwrap();
        vram.write(0x2000 + 0x8800 - 0x8000 + 64 + 10 + 0, 0b0100_0000)
            .unwrap();

        let ci = vram.lower_tile_bank0(1).and_then(|x| x.pixel(2, 3));
        assert_eq!(ci, Some(ColorIndex::One));

        let ci = vram.higher_tile_bank0(2).and_then(|x| x.pixel(3, 4));
        assert_eq!(ci, Some(ColorIndex::Two));

        let ci = vram.lower_tile_bank1(131).and_then(|x| x.pixel(4, 5));
        assert_eq!(ci, Some(ColorIndex::Three));

        let ci = vram.higher_tile_bank1(132).and_then(|x| x.pixel(5, 6));
        assert_eq!(ci, Some(ColorIndex::One));
    }

    #[test]
    fn tile_map() {
        let mut vram = Vram::new(true);
        vram.write(0x9800 - 0x8000 + 35, 5).unwrap();
        vram.write(0x9C00 - 0x8000 + 37, 6).unwrap();

        let idx = vram.lower_tile_map().and_then(|x| x.tile_idx(1, 3));
        assert_eq!(idx, Some(5));

        let idx = vram.higher_tile_map().and_then(|x| x.tile_idx(1, 5));
        assert_eq!(idx, Some(6));
    }
}
