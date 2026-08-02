use std::error::Error;

use boii::{
    bus::Bus,
    cartridge::{Cartridge, ParseConfig},
    memory::Write,
};

fn main() -> Result<(), Box<dyn Error>> {



    let err = boii::memory::Error::SegFault { address: 0x0100 };
    println!("{}", err);



    let rom = [0; 10];
    let rom_parse_config = ParseConfig::default();

    let cart = Cartridge::from(rom.into(), None, &rom_parse_config).unwrap();

    let mut bus = Bus::new(cart);

    bus.write(0, 0).unwrap();

    Ok(())
}
