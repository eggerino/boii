use crate::{
    memory::{Error, Read, Write},
    nums::U3,
};

pub struct Wram {
    offset: u16,
    buf: Vec<u8>,
}

impl Wram {
    pub fn new(cgb: bool) -> Self {
        let size = if cgb { 0x8000 } else { 0x2000 };

        Self {
            offset: 0,
            buf: vec![0; size],
        }
    }

    pub fn use_bank(&mut self, x: U3) {
        let x = match x {
            U3::Zero => 1,
            x => x.into(),
        } as u16;

        self.offset = x.saturating_sub(1).wrapping_mul(0x1000);
    }

    fn backing_address(&self, address: u16) -> u16 {
        if address < 0x1000 {
            address
        } else {
            address.wrapping_add(self.offset)
        }
    }
}

impl Read for Wram {
    fn read(&self, address: u16) -> Result<u8, Error> {
        self.buf.read(self.backing_address(address))
    }
}

impl Write for Wram {
    fn write(&mut self, address: u16, value: u8) -> Result<(), Error> {
        let address = self.backing_address(address);
        self.buf.write(address, value)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn wram() {
        let mut wram = Wram::new(true);
        wram.buf[0] = 1;
        wram.buf[0x1000] = 2;
        wram.buf[0x2000] = 3;
        wram.buf[0x3000] = 4;
        wram.buf[0x4000] = 5;
        wram.buf[0x5000] = 6;
        wram.buf[0x6000] = 7;
        wram.buf[0x7000] = 8;

        wram.use_bank(U3::Zero);
        assert_eq!(wram.read(0x0000), Ok(1));
        assert_eq!(wram.read(0x1000), Ok(2));

        wram.use_bank(U3::One);
        assert_eq!(wram.read(0x0000), Ok(1));
        assert_eq!(wram.read(0x1000), Ok(2));

        wram.use_bank(U3::Two);
        assert_eq!(wram.read(0x0000), Ok(1));
        assert_eq!(wram.read(0x1000), Ok(3));

        wram.use_bank(U3::Three);
        assert_eq!(wram.read(0x0000), Ok(1));
        assert_eq!(wram.read(0x1000), Ok(4));

        wram.use_bank(U3::Four);
        assert_eq!(wram.read(0x0000), Ok(1));
        assert_eq!(wram.read(0x1000), Ok(5));

        wram.use_bank(U3::Five);
        assert_eq!(wram.read(0x0000), Ok(1));
        assert_eq!(wram.read(0x1000), Ok(6));

        wram.use_bank(U3::Six);
        assert_eq!(wram.read(0x0000), Ok(1));
        assert_eq!(wram.read(0x1000), Ok(7));

        wram.use_bank(U3::Seven);
        assert_eq!(wram.read(0x0000), Ok(1));
        assert_eq!(wram.read(0x1000), Ok(8));
    }
}
