use std::error::Error;

use boii::{
    bus::Bus, cartridge::{Cartridge, ParseConfig}, cpu::Cpu, io::{Color, Draw, Gamepad}, window::Window,
};

fn main() -> Result<(), Box<dyn Error>> {
    let mut window = Window::new("Gameboyyyy");
    while !window.should_close() {

        for x in 0..100 {
            for y in 0..100 {
                let t = (x + y) % 4;
                let c = if t == 0 {
                    Color::White
                } else if t == 1 {
                    Color::LightGray
                } else if t == 2 {
                    Color::DarkGray
                } else {
                    Color::Black
                };
                window.set_pixel(x, y, c);
            }
        }
        window.update();

        println!("up {}", window.up());
        println!("down {}", window.down());
        println!("left {}", window.left());
        println!("right {}", window.right());
        println!("a {}", window.a());
        println!("b {}", window.b());
        println!("start {}", window.start());
        println!("select {}\n\n", window.select());
    }
    return Ok(());



    // TODO read the input of the emulation rom, battery safed ram ...
    let rom = [0; 10];
    let ram = None;
    let parse_config = ParseConfig::default();

    // Initialize the components of the gameboy
    let cart = Cartridge::from(rom.into(), ram, &parse_config)?;
    let window = Window::new(cart.title());
    let bus = Bus::new(cart);
    let mut cpu = Cpu::new(bus);

    // TODO run the emulation
    cpu.step()?;

    Ok(())
}
