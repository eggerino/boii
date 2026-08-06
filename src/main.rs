use boii::{
    bus::Bus,
    cartridge::{Cartridge, ParseConfig},
    cpu::Cpu,
    io::Draw,
    window::Window,
};
use clap::Parser;
use std::{error::Error, fs};

/// Gameboy emulator
#[derive(Parser)]
#[command(version, about)]
struct Args {
    /// Rom file to run the emulation with
    rom_file: String,

    /// File to use for persisting battery packed cartridge ram
    #[arg(short, long)]
    ram_file: Option<String>,

    /// Check the header for a valid nintendo logo before starting
    #[arg(long)]
    check_nintendo_logo: bool,

    /// Check the header checksum in the rom for validity before starting
    #[arg(long)]
    check_header_checksum: bool,

    /// Check the global checksum in the rom for validity before starting
    #[arg(long)]
    check_global_checksum: bool,
}

fn main() -> Result<(), Box<dyn Error>> {
    let args = Args::parse();

    let rom = fs::read(args.rom_file)?;
    let ram = invert(args.ram_file.as_ref().map(fs::read))?;
    let parse_config = ParseConfig {
        check_nintento_logo: args.check_nintendo_logo,
        check_header_checksum: args.check_header_checksum,
        check_global_checksum: args.check_global_checksum,
    };

    let cart = Cartridge::from(rom.into(), ram, &parse_config, args.ram_file)?;
    let mut window = Window::new(&format!("boii - {}", cart.title()));
    let bus = Bus::new(cart);
    let mut cpu = Cpu::new(bus);

    while !window.should_close() {
        cpu.step()?;
        window.update();
    }

    Ok(())
}

fn invert<T, E>(x: Option<Result<T, E>>) -> Result<Option<T>, E> {
    x.map_or(Ok(None), |r| r.map(Some))
}
