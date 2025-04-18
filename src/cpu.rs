mod registers;

use registers::Registers;
use crate::bus::Bus;

pub struct Cpu {
    registers: Registers,
    halted: bool,
    stepping: bool,
}

impl Cpu {
    pub fn new() -> Self {
        Self {
            registers: Registers::new(),
            halted: false,
            stepping: true,
        }
    }

    pub fn step(&mut self, bus: &mut Bus) -> bool {
        if self.halted {
            // fetch opcode
            // fetch data
            // execute
        }

        false
    }
}
