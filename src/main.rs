use std::{cell::RefCell, rc::Rc};

use boii::{
    bus::Bus,
    cartridge::{Cartridge, ParseConfig},
    memory::Write,
};

fn main() {
    let rom = [0; 10];
    let rom_parse_config = ParseConfig::default();

    let cart = Rc::new(RefCell::new(
        Cartridge::from(rom.into(), &rom_parse_config).unwrap(),
    ));

    let mut bus = Bus::new(cart);

    bus.write(0, 0).unwrap();
}
