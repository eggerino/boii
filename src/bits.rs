const HIGH_BYTE_MASK: u16 = 0xFF00;
const LOW_BYTE_MASK: u16 = 0x00FF;

pub type Bit = bool;

pub type BitPattern<const N: usize> = [Bit; N];

pub trait Bits<const N: usize>: Copy {
    fn bits_lsb_first(self) -> BitPattern<N>;
    fn bits_msb_first(self) -> BitPattern<N>;
    fn bit(self, idx: i32) -> Bit;
    fn set_bit(self, idx: i32) -> Self;
    fn clear_bit(self, idx: i32) -> Self;
}

impl Bits<8> for u8 {
    fn bits_lsb_first(self) -> BitPattern<8> {
        let mut x = [false; 8];
        x.iter_mut()
            .enumerate()
            .for_each(|(idx, cur)| *cur = (self & (1 << idx)) > 0);
        x
    }

    fn bits_msb_first(self) -> BitPattern<8> {
        let mut x = self.bits_lsb_first();
        x.reverse();
        x
    }

    #[inline]
    fn bit(self, idx: i32) -> Bit {
        self & (1 << idx) > 0
    }

    #[inline]
    fn set_bit(self, idx: i32) -> Self {
        self | (1 << idx)
    }

    #[inline]
    fn clear_bit(self, idx: i32) -> Self {
        self & !(1 << idx)
    }
}

impl Bits<16> for u16 {
    fn bits_lsb_first(self) -> BitPattern<16> {
        let mut x = [false; 16];
        x.iter_mut()
            .enumerate()
            .for_each(|(idx, cur)| *cur = (self & (1 << idx)) > 0);
        x
    }

    fn bits_msb_first(self) -> BitPattern<16> {
        let mut x = self.bits_lsb_first();
        x.reverse();
        x
    }

    #[inline]
    fn bit(self, idx: i32) -> Bit {
        self & (1 << idx) > 0
    }

    #[inline]
    fn set_bit(self, idx: i32) -> Self {
        self | (1 << idx)
    }

    #[inline]
    fn clear_bit(self, idx: i32) -> Self {
        self & !(1 << idx)
    }
}

#[inline]
pub fn low_byte(val: u16) -> u8 {
    val as u8
}

#[inline]
pub fn high_byte(val: u16) -> u8 {
    (val >> 8) as u8
}

#[inline]
pub fn combine_bytes(high: u8, low: u8) -> u16 {
    ((high as u16) << 8) | (low as u16)
}

#[inline]
pub fn set_low_byte(val: u16, low: u8) -> u16 {
    (val & HIGH_BYTE_MASK) | (low as u16)
}

#[inline]
pub fn set_high_byte(val: u16, high: u8) -> u16 {
    ((high as u16) << 8) | (val & LOW_BYTE_MASK)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_u8_lsb() {
        let x: u8 = 69;
        let bits = x.bits_lsb_first();
        assert_eq!(bits, [true, false, true, false, false, false, true, false]);
    }

    #[test]
    fn test_u8_msb_is_rev_of_lsb() {
        let x: u8 = 69;
        let mut bits = x.bits_lsb_first();
        let rev_bits = x.bits_msb_first();
        bits.reverse();
        assert_eq!(bits, rev_bits);
    }

    #[test]
    fn test_bit() {
        let x: u8 = 8;
        assert_eq!(x.bit(3), true);

        let x: u16 = 8;
        assert_eq!(x.bit(3), true);
    }

    #[test]
    fn test_set_bit() {
        let x: u8 = 0;
        assert_eq!(x.set_bit(3), 8);

        let x: u16 = 0;
        assert_eq!(x.set_bit(3), 8);
    }

    #[test]
    fn test_clear_bit() {
        let x: u8 = 8;
        assert_eq!(x.clear_bit(3), 0);

        let x: u16 = 8;
        assert_eq!(x.clear_bit(3), 0);
    }

    #[test]
    fn test_low_high_byte() {
        let x = 0x1234;
        let high = high_byte(x);
        let low = low_byte(x);
        assert_eq!(high, 0x12);
        assert_eq!(low, 0x34);
    }

    #[test]
    fn test_combine_bytes() {
        let x = combine_bytes(0x12, 0x34);
        assert_eq!(x, 0x1234);
    }

    #[test]
    fn test_set_low_high_byte() {
        let mut x = 0;
        x = set_low_byte(x, 0x34);
        x = set_high_byte(x, 0x12);
        assert_eq!(x, 0x1234);
    }
}
