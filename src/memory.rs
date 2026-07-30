#[derive(Debug, PartialEq)]
pub enum Error {
    SegFault { address: u16 },
}

type Result<T> = core::result::Result<T, Error>;

pub trait Read {
    fn read(&self, address: u16) -> Result<u8>;
}

pub trait Write {
    fn write(&mut self, address: u16, value: u8) -> Result<()>;
}

impl Read for [u8] {
    fn read(&self, address: u16) -> Result<u8> {
        self.get(address as usize)
            .map(|x| *x)
            .ok_or(Error::SegFault { address })
    }
}

impl Write for [u8] {
    fn write(&mut self, address: u16, value: u8) -> Result<()> {
        self.get_mut(address as usize)
            .map(|x| *x = value)
            .ok_or(Error::SegFault { address })
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn array_buffer_read_write() {
        let mut buf = [0; 24];

        assert_eq!(buf.read(0), Ok(0));
        assert_eq!(buf.read(23), Ok(0));
        assert_eq!(buf.read(24), Err(Error::SegFault { address: 24 }));

        assert_eq!(buf.write(0, 69), Ok(()));
        assert_eq!(buf.read(0), Ok(69));
        assert_eq!(buf.write(24, 5), Err(Error::SegFault { address: 24 }));
    }
}
