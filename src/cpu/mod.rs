// use crate::{
//     cpu::{imd::InterruptMasterDispatcher, registers::Registers},
//     memory::{Read, Write},
// };

// mod imd;
// mod instr;
// mod registers;

// pub struct Cpu<T>
// where
//     T: Read + Write,
// {
//     bus: T,
//     registers: Registers,
//     ticks: usize,
//     halted: bool,
//     interrupt_master: InterruptMasterDispatcher,
// }

// impl<T> Cpu<T>
// where
//     T: Read + Write,
// {
//     pub fn new(bus: T) -> Self {
//         Self {
//             bus,
//             registers: Registers::new(),
//             ticks: 0,
//             halted: false,
//             interrupt_master: InterruptMasterDispatcher::new(false),
//         }
//     }

//     pub fn step(&mut self) -> usize {
//         0
//     }
// }

// #[cfg(test)]
// mod tests {
//     use super::*;

//     type Cpu = super::Cpu<Vec<u8>>;

//     fn ensure_size(bus: &mut Vec<u8>, size: usize) {
//         if size > bus.len() {
//             bus.resize(size, 0);
//         }
//     }

//     fn step(cpu: &mut Cpu, amount: usize) -> usize {
//         (0..amount).fold(0, |acc, _| acc + cpu.step())
//     }

//     struct CpuState {
//         af: u16,
//         bc: u16,
//         de: u16,
//         hl: u16,
//         stack_ptr: u16,
//         prog_counter: u16,
//         interrupt_master: bool,
//         halted: bool,
//     }

//     fn cpu_state(
//         af: u16,
//         bc: u16,
//         de: u16,
//         hl: u16,
//         stack_ptr: u16,
//         prog_counter: u16,
//         interrupt_master: bool,
//         halted: bool,
//     ) -> CpuState {
//         CpuState {
//             af,
//             bc,
//             de,
//             hl,
//             stack_ptr,
//             prog_counter,
//             interrupt_master,
//             halted,
//         }
//     }

//     fn cpu_with_state(bus: Vec<u8>, state: CpuState) -> Cpu {
//         Cpu {
//             bus,
//             registers: Registers {
//                 af: state.af,
//                 bc: state.bc,
//                 de: state.de,
//                 hl: state.hl,
//                 stack_ptr: state.stack_ptr,
//                 prog_counter: state.prog_counter,
//             },
//             ticks: 0,
//             halted: state.halted,
//             interrupt_master: InterruptMasterDispatcher::new(state.interrupt_master),
//         }
//     }

//     fn assert_cpu(expected_ticks: usize, expected_state: &CpuState, actual: &Cpu) {
//         assert_eq!(expected_ticks, actual.ticks);
//         assert_eq!(expected_state.af, actual.registers.af);
//         assert_eq!(expected_state.bc, actual.registers.bc);
//         assert_eq!(expected_state.de, actual.registers.de);
//         assert_eq!(expected_state.hl, actual.registers.hl);
//         assert_eq!(expected_state.stack_ptr, actual.registers.stack_ptr);
//         assert_eq!(expected_state.prog_counter, actual.registers.prog_counter);
//         assert_eq!(
//             expected_state.interrupt_master,
//             actual.interrupt_master.value()
//         );
//         assert_eq!(expected_state.halted, actual.halted);
//     }

//     #[test]
//     fn cpu_halts until_interrupt() {
//         let mut bus = vec![0b0111_0110];  // halt
//         ensure_size(&mut bus, 0x1_0000);

//         let mut cpu = cpu_with_state(bus,  cpu_state(0, 0, 0, 0, 2, 0x0100,  true, false));

//         step(&mut cpu, 2);
//         assert_cpu(2, &cpu_state(0, 0, 0, 0, 2, 0x0101, true, true), &cpu);

//         bus.Write(0xFF0F, 0b0001_1111);
//         bus.Write(0xFFFF, 0b0001_1111);

//         cpu.Step();
//         AssertCpu(7, new(0, 0, 0, 0, 0, 0x0040, InterruptMaster: false, Halted: false), cpu);
//         Assert.Equal(0x01, bus.Read(0));
//         Assert.Equal(0x01, bus.Read(1));
//     }

//     // #[test]
//     // fn CpuWakesOnInterrupt()
//     // {
//     //     var bus = Bus.From([0b0111_0110]);  // halt
//     //     bus.EnsureSize(0x1_0000);
//     //     var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0, 0, 0, 0, 0x0100));

//     //     Step(cpu, 2);
//     //     AssertCpu(2, new(0, 0, 0, 0, 0, 0x0101, Halted: true), cpu);

//     //     bus.Write(0xFF0F, 0b0001_1111);
//     //     bus.Write(0xFFFF, 0b0001_1111);

//     //     cpu.Step();
//     //     AssertCpu(3, new(0, 0, 0, 0, 0, 0x0102), cpu);
//     // }

//     // #[test]
//     // fn HaltBug_RereadNextByte()
//     // {
//     //     var bus = Bus.From([
//     //         0b0111_0110,    // halt
//     //         0x06, 0x04,     // ld b, 4
//     //     ]);
//     //     bus.EnsureSize(0x1_0000);
//     //     bus.Write(0xFF0F, 0b0001_1111);
//     //     bus.Write(0xFFFF, 0b0001_1111);

//     //     var cpu = Cpu.Create(bus);

//     //     Step(cpu, 3);

//     //     // Cpu sees:
//     //     //      halt
//     //     //      ld b, 6
//     //     //      inc b
//     //     AssertCpu(4, new(0, 0x0700, 0, 0, 0, 0x0103, Halted: false), cpu);
//     // }

//     // #[test]
//     // fn HaltBug_HaltCanLoop()
//     // {
//     //     var bus = Bus.From([
//     //         0b0111_0110,    // halt
//     //         0b0111_0110,    // halt
//     //     ]);
//     //     bus.EnsureSize(0x1_0000);
//     //     bus.Write(0xFF0F, 0b0001_1111);
//     //     bus.Write(0xFFFF, 0b0001_1111);

//     //     var cpu = Cpu.Create(bus);

//     //     Step(cpu, 10);

//     //     AssertCpu(10, new(0, 0, 0, 0, 0, 0x0101, Halted: false), cpu);
//     // }

//     // #[test]
//     // fn HaltBug_EnableInterrupt_Halt_WillReturnToHalt()
//     // {
//     //     var bus = Bus.From([
//     //         0b1111_1011,        // ei
//     //         0b0111_0110,        // halt
//     //     ]);
//     //     bus.EnsureSize(0x1_0000);
//     //     bus.Write(0xFF0F, 0b0000_0001);
//     //     bus.Write(0xFFFF, 0b0000_0001);
//     //     bus.Write(0x0040, 0b1100_1001);     // Return the interrupt handler immediately
//     //     var cpu = Cpu.CreateWithRegisterState(bus, new(0, 0, 0, 0, 2, 0x0100));

//     //     Step(cpu, 4);
//     //     AssertCpu(11, new(0, 0, 0, 0, 2, 0x0101), cpu);
//     //     Assert.Equal(0x01, bus.Read(0));
//     //     Assert.Equal(0x01, bus.Read(1));
//     // }
// }
