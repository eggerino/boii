use boii::cartridge;
use std::env;

type Result<T> = core::result::Result<T, Box<dyn std::error::Error>>;

fn main() -> Result<()> {
    match &env::args().collect::<Vec<_>>()[..] {
        [prog] => usage(&prog),
        [_, rom_file] => run_rom(&rom_file),
        _ => unreachable!(),
    }
}

fn usage(prog: &str) -> Result<()> {
    Err(format!("Usage: {} <ROM-FILE>", prog).into())
}

fn run_rom(path: &str) -> Result<()> {
    let cart = cartridge::Cartridge::from_rom_file(
        path,
        &cartridge::ValidationSettings {
            use_global_checksum: false,
        },
    )?;
    Ok(())
}
