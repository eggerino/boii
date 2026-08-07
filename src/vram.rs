use crate::{
    bits::Bits,
    memory::{Error, Read, Write},
    nums::U3,
};

pub struct Vram {
    offset: u16,
    buf: Vec<u8>,
}

impl Read for Vram {
    fn read(&self, address: u16) -> Result<u8, Error> {
        self.buf.read(address.wrapping_add(self.offset))
    }
}

impl Write for Vram {
    fn write(&mut self, address: u16, value: u8) -> Result<(), Error> {
        self.buf.write(address.wrapping_add(self.offset), value)
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

    pub fn tile_bank0_lower(&self, idx: u8) -> Option<Tile<'_>> {
        let addr = 16_usize.wrapping_mul(idx as usize);
        self.tile(addr)
    }

    pub fn tile_bank0_higher(&self, idx: u8) -> Option<Tile<'_>> {
        let addr = 16_i32.wrapping_mul((idx as i8) as i32).wrapping_add(0x1000) as usize;
        self.tile(addr)
    }

    pub fn tile_bank1_lower(&self, idx: u8) -> Option<Tile<'_>> {
        let addr = 16_usize.wrapping_mul(idx as usize).wrapping_add(0x2000);
        self.tile(addr)
    }

    pub fn tile_bank1_higher(&self, idx: u8) -> Option<Tile<'_>> {
        let addr = 16_i32.wrapping_mul((idx as i8) as i32).wrapping_add(0x3000) as usize;
        self.tile(addr)
    }

    fn tile(&self, addr: usize) -> Option<Tile<'_>> {
        self.buf.get(addr..addr.saturating_add(16)).map(Tile)
    }

    pub fn tile_map_lower(&self) -> Option<TileMap<'_>> {
        self.tile_map(0x1800)
    }

    pub fn tile_map_higher(&self) -> Option<TileMap<'_>> {
        self.tile_map(0x1C00)
    }

    fn tile_map(&self, addr: usize) -> Option<TileMap<'_>> {
        self.buf.get(addr..addr.saturating_add(0x0400)).map(TileMap)
    }

    pub fn tile_attributes_map(&self) -> Option<TileAttributesMap<'_>> {
        self.buf.get(0x3800..0x3C00).map(TileAttributesMap)
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

fn tile_map_indexer(row: u8, col: u8) -> usize {
    32_u8.wrapping_mul(row).wrapping_add(col) as usize
}

pub struct TileMap<'a>(&'a [u8]);

impl<'a> TileMap<'a> {
    pub fn tile_idx(&self, row: u8, col: u8) -> Option<u8> {
        let index = tile_map_indexer(row, col);
        self.0.get(index).copied()
    }
}

pub struct TileAttributesMap<'a>(&'a [u8]);

impl<'a> TileAttributesMap<'a> {
    pub fn tile_attributes(&self, row: u8, col: u8) -> Option<TileAttributes> {
        let index = tile_map_indexer(row, col);
        self.0.get(index).copied().map(TileAttributes)
    }
}

pub struct TileAttributes(u8);

impl TileAttributes {
    pub fn priority(&self) -> bool {
        self.0.bit(7)
    }

    pub fn flip_y(&self) -> bool {
        self.0.bit(6)
    }

    pub fn flip_x(&self) -> bool {
        self.0.bit(5)
    }

    pub fn bank(&self) -> bool {
        self.0.bit(3)
    }

    pub fn color_palette(&self) -> U3 {
        self.0.into()
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

        let ci = vram.tile_bank0_lower(1).and_then(|x| x.pixel(2, 3));
        assert_eq!(ci, Some(ColorIndex::One));

        let ci = vram.tile_bank0_higher(2).and_then(|x| x.pixel(3, 4));
        assert_eq!(ci, Some(ColorIndex::Two));

        let ci = vram.tile_bank1_lower(131).and_then(|x| x.pixel(4, 5));
        assert_eq!(ci, Some(ColorIndex::Three));

        let ci = vram.tile_bank1_higher(132).and_then(|x| x.pixel(5, 6));
        assert_eq!(ci, Some(ColorIndex::One));
    }

    #[test]
    fn tile_map() {
        let mut vram = Vram::new(true);
        vram.write(0x9800 - 0x8000 + 35, 5).unwrap();
        vram.write(0x9C00 - 0x8000 + 37, 6).unwrap();

        let idx = vram.tile_map_lower().and_then(|x| x.tile_idx(1, 3));
        assert_eq!(idx, Some(5));

        let idx = vram.tile_map_higher().and_then(|x| x.tile_idx(1, 5));
        assert_eq!(idx, Some(6));
    }

    #[test]
    fn tile_attributes_map() {
        let mut vram = Vram::new(true);
        vram.write(0x3800 + 38, 69).unwrap();

        let attrs = vram
            .tile_attributes_map()
            .and_then(|x| x.tile_attributes(1, 6))
            .unwrap()
            .0;
        assert_eq!(attrs, 69);

        let attrs = TileAttributes(0);
        assert_eq!(attrs.priority(), false);
        assert_eq!(attrs.flip_y(), false);
        assert_eq!(attrs.flip_x(), false);
        assert_eq!(attrs.bank(), false);
        assert_eq!(attrs.color_palette(), U3::Zero);

        let attrs = TileAttributes(1);
        assert_eq!(attrs.priority(), false);
        assert_eq!(attrs.flip_y(), false);
        assert_eq!(attrs.flip_x(), false);
        assert_eq!(attrs.bank(), false);
        assert_eq!(attrs.color_palette(), U3::One);

        let attrs = TileAttributes(2);
        assert_eq!(attrs.priority(), false);
        assert_eq!(attrs.flip_y(), false);
        assert_eq!(attrs.flip_x(), false);
        assert_eq!(attrs.bank(), false);
        assert_eq!(attrs.color_palette(), U3::Two);

        let attrs = TileAttributes(4);
        assert_eq!(attrs.priority(), false);
        assert_eq!(attrs.flip_y(), false);
        assert_eq!(attrs.flip_x(), false);
        assert_eq!(attrs.bank(), false);
        assert_eq!(attrs.color_palette(), U3::Four);

        let attrs = TileAttributes(8);
        assert_eq!(attrs.priority(), false);
        assert_eq!(attrs.flip_y(), false);
        assert_eq!(attrs.flip_x(), false);
        assert_eq!(attrs.bank(), true);
        assert_eq!(attrs.color_palette(), U3::Zero);

        let attrs = TileAttributes(16);
        assert_eq!(attrs.priority(), false);
        assert_eq!(attrs.flip_y(), false);
        assert_eq!(attrs.flip_x(), false);
        assert_eq!(attrs.bank(), false);
        assert_eq!(attrs.color_palette(), U3::Zero);

        let attrs = TileAttributes(32);
        assert_eq!(attrs.priority(), false);
        assert_eq!(attrs.flip_y(), false);
        assert_eq!(attrs.flip_x(), true);
        assert_eq!(attrs.bank(), false);
        assert_eq!(attrs.color_palette(), U3::Zero);

        let attrs = TileAttributes(64);
        assert_eq!(attrs.priority(), false);
        assert_eq!(attrs.flip_y(), true);
        assert_eq!(attrs.flip_x(), false);
        assert_eq!(attrs.bank(), false);
        assert_eq!(attrs.color_palette(), U3::Zero);

        let attrs = TileAttributes(128);
        assert_eq!(attrs.priority(), true);
        assert_eq!(attrs.flip_y(), false);
        assert_eq!(attrs.flip_x(), false);
        assert_eq!(attrs.bank(), false);
        assert_eq!(attrs.color_palette(), U3::Zero);
    }
}
