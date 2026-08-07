use crate::{
    bits::Bits,
    memory::{Error, Read, Write},
    nums::U3,
};

pub struct Oam(Vec<u8>);

impl Read for Oam {
    fn read(&self, address: u16) -> Result<u8, Error> {
        self.0.read(address)
    }
}

impl Write for Oam {
    fn write(&mut self, address: u16, value: u8) -> Result<(), Error> {
        self.0.write(address, value)
    }
}

impl Oam {
    pub fn new() -> Self {
        Self(vec![0; 0xA0])
    }

    pub fn object_attributes(&self, idx: u8) -> Option<ObjectAttributes> {
        let addr = 4 * idx as usize;
        self.0
            .get(addr..addr + 4)
            .and_then(|x| x.try_into().ok())
            .map(ObjectAttributes)
    }
}

pub struct ObjectAttributes([u8; 4]);

impl ObjectAttributes {
    pub fn pos_y(&self) -> i32 {
        (self.0[0] as i32).wrapping_sub(16)
    }

    pub fn pos_x(&self) -> i32 {
        (self.0[1] as i32).wrapping_sub(8)
    }

    pub fn tile_idx(&self) -> u8 {
        self.0[2]
    }

    pub fn priority(&self) -> bool {
        self.0[3].bit(7)
    }

    pub fn flip_y(&self) -> bool {
        self.0[3].bit(6)
    }

    pub fn flip_x(&self) -> bool {
        self.0[3].bit(5)
    }

    pub fn dmg_palette(&self) -> bool {
        self.0[3].bit(4)
    }

    pub fn bank(&self) -> bool {
        self.0[3].bit(3)
    }

    pub fn cgb_palette(&self) -> U3 {
        self.0[3].into()
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn oam() {
        let mut oam = Oam::new();

        oam.write(4, 5).unwrap();
        oam.write(5, 6).unwrap();
        oam.write(6, 7).unwrap();

        oam.write(11, 0b1000_0000).unwrap();
        oam.write(15, 0b0100_0000).unwrap();
        oam.write(19, 0b0010_0000).unwrap();
        oam.write(23, 0b0001_0000).unwrap();
        oam.write(27, 0b0000_1000).unwrap();
        oam.write(31, 0b0000_0100).unwrap();
        oam.write(35, 0b0000_0010).unwrap();
        oam.write(39, 0b0000_0001).unwrap();

        assert_eq!(oam.object_attributes(1).unwrap().pos_y(), -11);
        assert_eq!(oam.object_attributes(1).unwrap().pos_x(), -2);
        assert_eq!(oam.object_attributes(1).unwrap().tile_idx(), 7);
        assert_eq!(oam.object_attributes(1).unwrap().priority(), false);
        assert_eq!(oam.object_attributes(1).unwrap().flip_y(), false);
        assert_eq!(oam.object_attributes(1).unwrap().flip_x(), false);
        assert_eq!(oam.object_attributes(1).unwrap().dmg_palette(), false);
        assert_eq!(oam.object_attributes(1).unwrap().bank(), false);
        assert_eq!(oam.object_attributes(1).unwrap().cgb_palette(), U3::Zero);

        assert_eq!(oam.object_attributes(2).unwrap().priority(), true);
        assert_eq!(oam.object_attributes(2).unwrap().flip_y(), false);
        assert_eq!(oam.object_attributes(2).unwrap().flip_x(), false);
        assert_eq!(oam.object_attributes(2).unwrap().dmg_palette(), false);
        assert_eq!(oam.object_attributes(2).unwrap().bank(), false);
        assert_eq!(oam.object_attributes(2).unwrap().cgb_palette(), U3::Zero);

        assert_eq!(oam.object_attributes(3).unwrap().priority(), false);
        assert_eq!(oam.object_attributes(3).unwrap().flip_y(), true);
        assert_eq!(oam.object_attributes(3).unwrap().flip_x(), false);
        assert_eq!(oam.object_attributes(3).unwrap().dmg_palette(), false);
        assert_eq!(oam.object_attributes(3).unwrap().bank(), false);
        assert_eq!(oam.object_attributes(3).unwrap().cgb_palette(), U3::Zero);

        assert_eq!(oam.object_attributes(4).unwrap().priority(), false);
        assert_eq!(oam.object_attributes(4).unwrap().flip_y(), false);
        assert_eq!(oam.object_attributes(4).unwrap().flip_x(), true);
        assert_eq!(oam.object_attributes(4).unwrap().dmg_palette(), false);
        assert_eq!(oam.object_attributes(4).unwrap().bank(), false);
        assert_eq!(oam.object_attributes(4).unwrap().cgb_palette(), U3::Zero);

        assert_eq!(oam.object_attributes(5).unwrap().priority(), false);
        assert_eq!(oam.object_attributes(5).unwrap().flip_y(), false);
        assert_eq!(oam.object_attributes(5).unwrap().flip_x(), false);
        assert_eq!(oam.object_attributes(5).unwrap().dmg_palette(), true);
        assert_eq!(oam.object_attributes(5).unwrap().bank(), false);
        assert_eq!(oam.object_attributes(5).unwrap().cgb_palette(), U3::Zero);

        assert_eq!(oam.object_attributes(6).unwrap().priority(), false);
        assert_eq!(oam.object_attributes(6).unwrap().flip_y(), false);
        assert_eq!(oam.object_attributes(6).unwrap().flip_x(), false);
        assert_eq!(oam.object_attributes(6).unwrap().dmg_palette(), false);
        assert_eq!(oam.object_attributes(6).unwrap().bank(), true);
        assert_eq!(oam.object_attributes(6).unwrap().cgb_palette(), U3::Zero);

        assert_eq!(oam.object_attributes(7).unwrap().priority(), false);
        assert_eq!(oam.object_attributes(7).unwrap().flip_y(), false);
        assert_eq!(oam.object_attributes(7).unwrap().flip_x(), false);
        assert_eq!(oam.object_attributes(7).unwrap().dmg_palette(), false);
        assert_eq!(oam.object_attributes(7).unwrap().bank(), false);
        assert_eq!(oam.object_attributes(7).unwrap().cgb_palette(), U3::Four);

        assert_eq!(oam.object_attributes(8).unwrap().priority(), false);
        assert_eq!(oam.object_attributes(8).unwrap().flip_y(), false);
        assert_eq!(oam.object_attributes(8).unwrap().flip_x(), false);
        assert_eq!(oam.object_attributes(8).unwrap().dmg_palette(), false);
        assert_eq!(oam.object_attributes(8).unwrap().bank(), false);
        assert_eq!(oam.object_attributes(8).unwrap().cgb_palette(), U3::Two);

        assert_eq!(oam.object_attributes(9).unwrap().priority(), false);
        assert_eq!(oam.object_attributes(9).unwrap().flip_y(), false);
        assert_eq!(oam.object_attributes(9).unwrap().flip_x(), false);
        assert_eq!(oam.object_attributes(9).unwrap().dmg_palette(), false);
        assert_eq!(oam.object_attributes(9).unwrap().bank(), false);
        assert_eq!(oam.object_attributes(9).unwrap().cgb_palette(), U3::One);
    }
}
