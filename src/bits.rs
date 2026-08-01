pub type BitPattern<const N: usize> = [bool; N];

pub trait Bits<const N: usize> {
    fn bits_lsb_first(&self) -> BitPattern<N>;

    fn bits_msb_first(&self) -> BitPattern<N> {
        let mut x = self.bits_lsb_first();
        x.reverse();
        x
    }
}

impl Bits<8> for u8 {
    fn bits_lsb_first(&self) -> BitPattern<8> {
        let mut x = [false; 8];
        x.iter_mut()
            .enumerate()
            .for_each(|(idx, cur)| *cur = (self & (1 << idx)) > 0);
        x
    }
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
}
