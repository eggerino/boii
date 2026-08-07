use crate::bits::{BitPattern, Bits};

#[derive(Debug, PartialEq)]
pub enum U2 {
    Zero = 0,
    One = 1,
    Two = 2,
    Three = 3,
}

impl U2 {
    pub fn from_lsb_first(pattern: BitPattern<2>) -> Self {
        match pattern {
            [false, false] => Self::Zero,
            [true, false] => Self::One,
            [false, true] => Self::Two,
            [true, true] => Self::Three,
        }
    }

    pub fn from_msb_first(mut pattern: BitPattern<2>) -> Self {
        pattern.reverse();
        Self::from_lsb_first(pattern)
    }
}

impl From<U2> for u8 {
    fn from(value: U2) -> Self {
        match value {
            U2::Zero => 0,
            U2::One => 1,
            U2::Two => 2,
            U2::Three => 3,
        }
    }
}

impl From<u8> for U2 {
    fn from(value: u8) -> Self {
        let [x0, x1, _, _, _, _, _, _] = value.bits_lsb_first();
        Self::from_lsb_first([x0, x1])
    }
}

#[derive(Debug, PartialEq)]
pub enum U3 {
    Zero = 0,
    One = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
}

impl U3 {
    pub fn from_lsb_first(pattern: BitPattern<3>) -> Self {
        match pattern {
            [false, false, false] => U3::Zero,
            [true, false, false] => U3::One,
            [false, true, false] => U3::Two,
            [true, true, false] => U3::Three,
            [false, false, true] => U3::Four,
            [true, false, true] => U3::Five,
            [false, true, true] => U3::Six,
            [true, true, true] => U3::Seven,
        }
    }

    pub fn from_msb_first(mut pattern: BitPattern<3>) -> Self {
        pattern.reverse();
        Self::from_lsb_first(pattern)
    }
}

impl From<U3> for u8 {
    fn from(val: U3) -> Self {
        match val {
            U3::Zero => 0,
            U3::One => 1,
            U3::Two => 2,
            U3::Three => 3,
            U3::Four => 4,
            U3::Five => 5,
            U3::Six => 6,
            U3::Seven => 7,
        }
    }
}

impl From<u8> for U3 {
    fn from(val: u8) -> Self {
        let [x0, x1, x2, _, _, _, _, _] = val.bits_lsb_first();
        Self::from_lsb_first([x0, x1, x2])
    }
}
