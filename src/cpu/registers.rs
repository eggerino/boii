use crate::bits::{Bits, high_byte, low_byte, set_high_byte, set_low_byte};

const ZERO_FLAG_IDX: i32 = 7;
const SUB_FLAG_IDX: i32 = 6;
const HALF_CARRY_FLAG_IDX: i32 = 5;
const CARRY_FLAG_IDX: i32 = 4;

pub struct Registers {
    pub af: u16,
    pub bc: u16,
    pub de: u16,
    pub hl: u16,
    pub stack_ptr: u16,
    pub prog_counter: u16,
}

impl Default for Registers {
    fn default() -> Self {
        Self {
            af: 0,
            bc: 0,
            de: 0,
            hl: 0,
            stack_ptr: 0,
            prog_counter: 0x0100,
        }
    }
}

impl Registers {
    #[inline]
    pub fn get_a(&self) -> u8 {
        high_byte(self.af)
    }

    #[inline]
    pub fn set_a(&mut self, value: u8) {
        self.af = set_high_byte(self.af, value);
    }

    #[inline]
    pub fn get_zero_flag(&self) -> bool {
        self.af.bit(ZERO_FLAG_IDX)
    }

    #[inline]
    pub fn set_zero_flag(&mut self) {
        self.af = self.af.set_bit(ZERO_FLAG_IDX);
    }

    #[inline]
    pub fn clear_zero_flag(&mut self) {
        self.af = self.af.clear_bit(ZERO_FLAG_IDX);
    }

    #[inline]
    pub fn get_sub_flag(&self) -> bool {
        self.af.bit(SUB_FLAG_IDX)
    }

    #[inline]
    pub fn set_sub_flag(&mut self) {
        self.af = self.af.set_bit(SUB_FLAG_IDX);
    }

    #[inline]
    pub fn clear_sub_flag(&mut self) {
        self.af = self.af.clear_bit(SUB_FLAG_IDX);
    }

    #[inline]
    pub fn get_half_carry_flag(&self) -> bool {
        self.af.bit(HALF_CARRY_FLAG_IDX)
    }

    #[inline]
    pub fn set_half_carry_flag(&mut self) {
        self.af = self.af.set_bit(HALF_CARRY_FLAG_IDX);
    }

    #[inline]
    pub fn clear_half_carry_flag(&mut self) {
        self.af = self.af.clear_bit(HALF_CARRY_FLAG_IDX);
    }

    #[inline]
    pub fn get_carry_flag(&self) -> bool {
        self.af.bit(CARRY_FLAG_IDX)
    }

    #[inline]
    pub fn set_carry_flag(&mut self) {
        self.af = self.af.set_bit(CARRY_FLAG_IDX);
    }

    #[inline]
    pub fn clear_carry_flag(&mut self) {
        self.af = self.af.clear_bit(CARRY_FLAG_IDX);
    }

    #[inline]
    pub fn get_b(&self) -> u8 {
        high_byte(self.bc)
    }

    #[inline]
    pub fn set_b(&mut self, value: u8) {
        self.bc = set_high_byte(self.bc, value);
    }

    #[inline]
    pub fn get_c(&self) -> u8 {
        low_byte(self.bc)
    }

    #[inline]
    pub fn set_c(&mut self, value: u8) {
        self.bc = set_low_byte(self.bc, value);
    }

    #[inline]
    pub fn get_d(&self) -> u8 {
        high_byte(self.de)
    }

    #[inline]
    pub fn set_d(&mut self, value: u8) {
        self.de = set_high_byte(self.de, value);
    }

    #[inline]
    pub fn get_e(&self) -> u8 {
        low_byte(self.de)
    }

    #[inline]
    pub fn set_e(&mut self, value: u8) {
        self.de = set_low_byte(self.de, value);
    }

    #[inline]
    pub fn get_h(&self) -> u8 {
        high_byte(self.hl)
    }

    #[inline]
    pub fn set_h(&mut self, value: u8) {
        self.hl = set_high_byte(self.hl, value);
    }

    #[inline]
    pub fn get_l(&self) -> u8 {
        low_byte(self.hl)
    }

    #[inline]
    pub fn set_l(&mut self, value: u8) {
        self.hl = set_low_byte(self.hl, value);
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn af_reg() {
        let mut r = Registers::default();

        r.af = 0x1200;
        assert_eq!(r.get_a(), 0x12);

        r.af = 0x0080;
        assert_eq!(r.get_zero_flag(), true);

        r.af = 0x0040;
        assert_eq!(r.get_sub_flag(), true);

        r.af = 0x0020;
        assert_eq!(r.get_half_carry_flag(), true);

        r.af = 0x0010;
        assert_eq!(r.get_carry_flag(), true);

        r.af = 0;
        r.set_a(0xAB);
        assert_eq!(r.af, 0xAB00);

        r.set_carry_flag();
        assert_eq!(r.af, 0xAB10);

        r.set_half_carry_flag();
        assert_eq!(r.af, 0xAB30);

        r.set_sub_flag();
        assert_eq!(r.af, 0xAB70);

        r.set_zero_flag();
        assert_eq!(r.af, 0xABF0);

        r.clear_carry_flag();
        assert_eq!(r.af, 0xABE0);

        r.clear_half_carry_flag();
        assert_eq!(r.af, 0xABC0);

        r.clear_sub_flag();
        assert_eq!(r.af, 0xAB80);

        r.clear_zero_flag();
        assert_eq!(r.af, 0xAB00);
    }

    #[test]
    fn bc_reg() {
        let mut r = Registers::default();

        r.bc = 0x0102;
        assert_eq!(r.get_b(), 0x01);
        assert_eq!(r.get_c(), 0x02);

        r.set_b(0x03);
        r.set_c(0x04);
        assert_eq!(r.bc, 0x0304);
    }

    #[test]
    fn de_reg() {
        let mut r = Registers::default();

        r.de = 0x0102;
        assert_eq!(r.get_d(), 0x01);
        assert_eq!(r.get_e(), 0x02);

        r.set_d(0x03);
        r.set_e(0x04);
        assert_eq!(r.de, 0x0304);
    }

    #[test]
    fn hl_reg() {
        let mut r = Registers::default();

        r.hl = 0x0102;
        assert_eq!(r.get_h(), 0x01);
        assert_eq!(r.get_l(), 0x02);

        r.set_h(0x03);
        r.set_l(0x04);
        assert_eq!(r.hl, 0x0304);
    }
}
