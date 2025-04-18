pub struct Registers {
    pub a: u8,
    pub f: u8,
    pub b: u8,
    pub c: u8,
    pub d: u8,
    pub e: u8,
    pub h: u8,
    pub l: u8,
    pub stack_pointer: u16,
    pub program_counter: u16,
}

impl Registers {
    // Factory
    pub fn new() -> Self {
        Self {
            a: 0,
            f: 0,
            b: 0,
            c: 0,
            d: 0,
            e: 0,
            h: 0,
            l: 0,
            stack_pointer: 0,
            program_counter: 0x0100,
        }
    }

    // u16 registers getters and setters
    pub fn get_af(&self) -> u16 {
        to_u16(self.a, self.f)
    }

    pub fn get_bc(&self) -> u16 {
        to_u16(self.b, self.c)
    }

    pub fn get_de(&self) -> u16 {
        to_u16(self.d, self.e)
    }

    pub fn get_hl(&self) -> u16 {
        to_u16(self.h, self.l)
    }

    pub fn set_af(&mut self, value: u16) {
        (self.a, self.f) = to_u8(value);
    }

    pub fn set_bc(&mut self, value: u16) {
        (self.b, self.c) = to_u8(value);
    }

    pub fn set_de(&mut self, value: u16) {
        (self.d, self.e) = to_u8(value);
    }

    pub fn set_hl(&mut self, value: u16) {
        (self.h, self.l) = to_u8(value);
    }

    // Flag getters and setters
    pub fn get_zero_flag(&self) -> bool {
        get_bit(self.f, 7)
    }

    pub fn get_subtraction_flag(&self) -> bool {
        get_bit(self.f, 6)
    }

    pub fn get_half_carry_flag(&self) -> bool {
        get_bit(self.f, 5)
    }

    pub fn get_carry_flag(&self) -> bool {
        get_bit(self.f, 4)
    }

    pub fn set_zero_flag(&mut self, value: bool) {
        set_bit(7, value, &mut self.f)
    }

    pub fn set_subtraction_flag(&mut self, value: bool) {
        set_bit(6, value, &mut self.f)
    }

    pub fn set_half_carry_flag(&mut self, value: bool) {
        set_bit(5, value, &mut self.f)
    }

    pub fn set_carry_flag(&mut self, value: bool) {
        set_bit(4, value, &mut self.f)
    }
}

fn to_u16(high: u8, low: u8) -> u16 {
    (high as u16) << 8 | (low as u16)
}

fn to_u8(value: u16) -> (u8, u8) {
    ((value >> 8) as u8, value as u8)
}

fn get_bit(value: u8, index: u8) -> bool {
    ((value >> index) & 1) == 1
}

fn set_bit(index: u8, bit: bool, value: &mut u8) {
    if bit {
        *value |= 1 << index;
    } else {
        *value &= !(1 << index);
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn get_af() {
        let mut reg = Registers::new();
        reg.a = 2;
        reg.f = 1;

        assert_eq!(0x0201, reg.get_af());
    }

    #[test]
    fn get_bc() {
        let mut reg = Registers::new();
        reg.b = 2;
        reg.c = 1;

        assert_eq!(0x0201, reg.get_bc());
    }

    #[test]
    fn get_de() {
        let mut reg = Registers::new();
        reg.d = 2;
        reg.e = 1;

        assert_eq!(0x0201, reg.get_de());
    }

    #[test]
    fn get_hl() {
        let mut reg = Registers::new();
        reg.h = 2;
        reg.l = 1;

        assert_eq!(0x0201, reg.get_hl());
    }

    #[test]
    fn set_af() {
        let mut reg = Registers::new();

        reg.set_af(0x0201);

        assert_eq!(2, reg.a);
        assert_eq!(1, reg.f);
    }

    #[test]
    fn set_bc() {
        let mut reg = Registers::new();

        reg.set_bc(0x0201);

        assert_eq!(2, reg.b);
        assert_eq!(1, reg.c);
    }

    #[test]
    fn set_de() {
        let mut reg = Registers::new();

        reg.set_de(0x0201);

        assert_eq!(2, reg.d);
        assert_eq!(1, reg.e);
    }

    #[test]
    fn set_hl() {
        let mut reg = Registers::new();

        reg.set_hl(0x0201);

        assert_eq!(2, reg.h);
        assert_eq!(1, reg.l);
    }

    #[test]
    fn get_zero_flag() {
        let mut reg = Registers::new();
        reg.f = 0b1000_0000;

        assert_eq!(true, reg.get_zero_flag());
    }

    #[test]
    fn get_subtraction_flag() {
        let mut reg = Registers::new();
        reg.f = 0b0100_0000;

        assert_eq!(true, reg.get_subtraction_flag());
    }

    #[test]
    fn get_half_carry_flag() {
        let mut reg = Registers::new();
        reg.f = 0b0010_0000;

        assert_eq!(true, reg.get_half_carry_flag());
    }

    #[test]
    fn get_carry_flag() {
        let mut reg = Registers::new();
        reg.f = 0b0001_0000;

        assert_eq!(true, reg.get_carry_flag());
    }

    #[test]
    fn set_zero_flag() {
        let mut reg = Registers::new();

        reg.set_zero_flag(true);

        assert_eq!(0b1000_0000, reg.f);
    }

    #[test]
    fn set_substraction_flag() {
        let mut reg = Registers::new();

        reg.set_subtraction_flag(true);

        assert_eq!(0b0100_0000, reg.f);
    }

    #[test]
    fn set_half_carry_flag() {
        let mut reg = Registers::new();

        reg.set_half_carry_flag(true);

        assert_eq!(0b0010_0000, reg.f);
    }

    #[test]
    fn set_carry_flag() {
        let mut reg = Registers::new();

        reg.set_carry_flag(true);

        assert_eq!(0b0001_0000, reg.f);
    }
}
