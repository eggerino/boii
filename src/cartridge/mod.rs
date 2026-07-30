use crate::{
    cartridge::header::Header,
    memory::{Error, Read, Write},
};

mod header;

pub use header::{ParseConfig, ParseError};

pub struct Cartridge {
    header: Header,
    rom: Rom,
    ram: Option<Ram>,
}

impl Cartridge {
    pub fn from(rom: Box<[u8]>, config: &ParseConfig) -> Result<Self, ParseError> {
        let header = Header::parse(&rom, config)?;
        let rom = Rom::from(rom);
        let ram = if header.cartridge_type.ram {
            Some(Ram::new(header.ram_size))
        } else {
            None
        };

        Ok(Self { header, rom, ram })
    }

    pub fn title(&self) -> &str {
        &self.header.title
    }

    pub fn rom(&self) -> &Rom {
        &self.rom
    }

    pub fn ram(&self) -> Option<&Ram> {
        self.ram.as_ref()
    }

    pub fn ram_mut(&mut self) -> Option<&mut Ram> {
        self.ram.as_mut()
    }
}

pub struct Rom(Box<[u8]>);

impl Rom {
    fn from(buffer: Box<[u8]>) -> Self {
        Self(buffer)
    }
}

impl Read for Rom {
    fn read(&self, address: u16) -> Result<u8, Error> {
        self.0.read(address)
    }
}

pub struct Ram(Vec<u8>);

impl Ram {
    fn new(size: usize) -> Self {
        Self(vec![0; size])
    }
}

impl Read for Ram {
    fn read(&self, address: u16) -> Result<u8, Error> {
        self.0.read(address)
    }
}

impl Write for Ram {
    fn write(&mut self, address: u16, value: u8) -> Result<(), Error> {
        self.0.write(address, value)
    }
}
