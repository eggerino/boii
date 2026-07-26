use boii::memory::{ArrayBuffer, Rom};

fn main() {
    let size = 1024;
    let buf = ArrayBuffer::new(size);



    println!("Hello {}!", read(&buf));
}

fn read(rom: &impl Rom) -> u8 {
    rom.read(0).unwrap()
}