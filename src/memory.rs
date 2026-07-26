#[derive(Debug, PartialEq)]
pub enum Error {
    SegFault { address: u16 },
}

pub type Result<T> = core::result::Result<T, Error>;

pub trait Rom {
    fn read(&self, address: u16) -> Result<u8>;
}

pub trait Ram: Rom {
    fn write(&mut self, address: u16, value: u8) -> Result<()>;
}

pub struct ArrayBuffer {
    buffer: Vec<u8>,
}

impl ArrayBuffer {
    pub fn new(size: u16) -> Self {
        Self {
            buffer: vec![0; size.into()],
        }
    }
}

impl Rom for ArrayBuffer {
    fn read(&self, address: u16) -> Result<u8> {
        if address as usize >= self.buffer.len() {
            Err(Error::SegFault { address })
        } else {
            Ok(self.buffer[address as usize])
        }
    }
}

impl Ram for ArrayBuffer {
    fn write(&mut self, address: u16, value: u8) -> Result<()> {
        if address as usize >= self.buffer.len() {
            Err(Error::SegFault { address })
        } else {
            self.buffer[address as usize] = value;
            Ok(())
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn array_buffer_read_write() {
        let mut buf = ArrayBuffer::new(24);

        assert_eq!(buf.read(0), Ok(0));
        assert_eq!(buf.read(23), Ok(0));
        assert_eq!(buf.read(24), Err(Error::SegFault { address: 24 }));

        assert_eq!(buf.write(0, 69), Ok(()));
        assert_eq!(buf.read(0), Ok(69));
        assert_eq!(buf.write(24, 5), Err(Error::SegFault { address: 24 }));
    }
}
