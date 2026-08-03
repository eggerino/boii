mod arithmetic16bit;
mod arithmetic8bit;
mod bit_shift;
mod bitwise_logic;
mod interrupt;
mod jump;
mod load;
mod misc;

use super::*;
use crate::memory::Error;
use std::ops::{Deref, DerefMut};

// Mock bus with a plain Vec<u8>
impl Read for Vec<u8> {
    fn read(&self, address: u16) -> core::result::Result<u8, Error> {
        self.deref().read(address)
    }
}

impl Write for Vec<u8> {
    fn write(&mut self, address: u16, value: u8) -> core::result::Result<(), Error> {
        self.deref_mut().write(address, value)
    }
}

// Constructor for a mocked bus containing a program at the usual start address
fn prog<const N: usize>(src: [u8; N]) -> Vec<u8> {
    let mut memory = vec![0; 0x0100];
    memory.extend_from_slice(&src);
    memory
}

fn ensure_size(mem: &mut Vec<u8>, size: usize) {
    if mem.len() < size {
        mem.resize(size, 0);
    }
}

// Compare cpu via the effective state
#[derive(Debug, PartialEq)]
struct State {
    af: u16,
    bc: u16,
    de: u16,
    hl: u16,
    stack_ptr: u16,
    prog_counter: u16,
    halted: bool,
    imd: bool,
}

fn state(
    af: u16,
    bc: u16,
    de: u16,
    hl: u16,
    stack_ptr: u16,
    prog_counter: u16,
    halted: bool,
    imd: bool,
) -> State {
    State {
        af,
        bc,
        de,
        hl,
        stack_ptr,
        prog_counter,
        halted,
        imd,
    }
}

fn cpu(bus: Vec<u8>, state: State) -> Cpu<Vec<u8>> {
    Cpu {
        bus,
        reg: Registers {
            af: state.af,
            bc: state.bc,
            de: state.de,
            hl: state.hl,
            stack_ptr: state.stack_ptr,
            prog_counter: state.prog_counter,
        },
        ticks: 0,
        halted: state.halted,
        imd: InterruptMasterDispatcher::new_with(state.imd),
        buffered_opcode: None,
    }
}

fn assert_cpu<T>(expected_ticks: usize, expected_state: State, actual: &Cpu<T>)
where
    T: Read + Write,
{
    assert_eq!(expected_ticks, actual.ticks);
    assert_eq!(expected_state.af, actual.reg.af);
    assert_eq!(expected_state.bc, actual.reg.bc);
    assert_eq!(expected_state.de, actual.reg.de);
    assert_eq!(expected_state.hl, actual.reg.hl);
    assert_eq!(expected_state.stack_ptr, actual.reg.stack_ptr);
    assert_eq!(expected_state.prog_counter, actual.reg.prog_counter);
    assert_eq!(expected_state.halted, actual.halted);
    assert_eq!(expected_state.imd, actual.imd.value());
}

fn step<T>(cpu: &mut Cpu<T>, amount: usize) -> usize
where
    T: Read + Write,
{
    (0..amount).fold(0, |acc, _| acc + cpu.step().unwrap())
}
