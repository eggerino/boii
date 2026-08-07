mod imd;
mod instr;
mod registers;

use crate::{
    bits::{Bits, combine_bytes, high_byte, low_byte},
    cpu::{
        imd::InterruptMasterDispatcher,
        instr::{
            Condition, Instruction, PrefixedInstruction, Register8, Register16, Register16Memory,
            Register16Stack,
        },
        registers::Registers,
    },
    memory::{self, Read, Write},
    nums::U3,
};

const INTERRUPT_FLAG_ADDR: u16 = 0xFF0F;
const INTERRUPT_ENABLE_ADDR: u16 = 0xFFFF;

#[derive(Debug, PartialEq)]
pub enum Error {
    InvalidInstruction { opcode: u8 },
    SegFault { address: u16 },
}

impl core::fmt::Display for Error {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Error::InvalidInstruction { opcode } => write!(
                f,
                "The invalid opcode {:#x} was tried to be executed.",
                opcode
            ),
            Error::SegFault { address } => memory::Error::SegFault { address: *address }.fmt(f),
        }
    }
}

impl core::error::Error for Error {}

impl From<memory::Error> for Error {
    fn from(value: memory::Error) -> Self {
        match value {
            memory::Error::SegFault { address } => Self::SegFault { address },
        }
    }
}

type Result<T> = core::result::Result<T, Error>;

pub struct Cpu<T>
where
    T: Read + Write,
{
    bus: T,
    reg: Registers,
    ticks: usize,
    halted: bool,
    imd: InterruptMasterDispatcher,
    buffered_opcode: Option<u8>,
}

impl<T> Cpu<T>
where
    T: Read + Write,
{
    pub fn new(bus: T) -> Self {
        Self {
            bus,
            reg: Registers::default(),
            ticks: 0,
            halted: false,
            imd: InterruptMasterDispatcher::new(),
            buffered_opcode: None,
        }
    }

    pub fn ticks(&self) -> usize {
        self.ticks
    }

    pub fn step(&mut self) -> Result<usize> {
        self.advance()
            .inspect(|&x| self.ticks = self.ticks.wrapping_add(x))
    }

    fn advance(&mut self) -> Result<usize> {
        // REFACTOR
        if let Some(ticks) = self.handle_interrupt()? {
            return Ok(ticks);
        }

        if let Some(ticks) = self.handle_halt()? {
            return Ok(ticks);
        }

        let opcode = self.get_opcode()?;
        let inst = Instruction::from(opcode);

        let ticks = self.execute(inst);
        self.imd.update();

        ticks
    }

    // Halt & Interrupts
    fn handle_interrupt(&mut self) -> Result<Option<usize>> {
        if !self.imd.value() {
            return Ok(None);
        }

        let pending = self.pending_interrupts()?;
        if pending == 0 {
            return Ok(None);
        }

        self.execute_interrupt(pending).map(|_| Some(5))
    }

    fn handle_halt(&mut self) -> Result<Option<usize>> {
        if !self.halted {
            return Ok(None);
        }

        if self.imd.value() {
            return Ok(Some(1)); // wait for an interrupt
        }

        if self.pending_interrupts()? != 0 {
            self.halted = false; // Wake up on pending interrupts (but not handle it)
            return Ok(None);
        }

        Ok(Some(1)) // Keep in halting state
    }

    fn pending_interrupts(&self) -> Result<u8> {
        self.bus
            .read(INTERRUPT_ENABLE_ADDR)
            .and_then(|e| self.bus.read(INTERRUPT_FLAG_ADDR).map(|f| e & f))
            .map_err(|x| x.into())
    }

    fn execute_interrupt(&mut self, pending: u8) -> Result<()> {
        // Get the interrupt with the highest priority
        let idx = (0..5)
            .map(|i| (i, pending.bit(i)))
            .filter(|&(_, x)| x)
            .map(|(x, _)| x)
            .next();

        if let Some(idx) = idx {
            // Disable its requested flag
            self.ack_interrupt(idx)?;

            // Disable master flag immediately (Prevent others)
            self.imd.force(false);

            // Call the address of the interrupt
            let addr = 0x0040_u16.wrapping_add((idx as u16).wrapping_mul(0x0008));
            self.do_call(addr)?;

            // Interrupts resume execution
            self.halted = false;
        }

        Ok(())
    }

    fn ack_interrupt(&mut self, idx: i32) -> Result<()> {
        self.bus
            .read(INTERRUPT_FLAG_ADDR)
            .map(|f| f.clear_bit(idx))
            .and_then(|f| self.bus.write(INTERRUPT_FLAG_ADDR, f))
            .map_err(|x| x.into())
    }

    // Execution utility
    fn get_opcode(&mut self) -> Result<u8> {
        self.buffered_opcode
            .take()
            .map(Ok)
            .unwrap_or_else(|| self.fetch_u8())
    }

    fn fetch_u8(&mut self) -> Result<u8> {
        self.bus
            .read(self.reg.prog_counter)
            .inspect(|_| self.reg.prog_counter = self.reg.prog_counter.wrapping_add(1))
            .map_err(|x| x.into())
    }

    fn fetch_u16(&mut self) -> Result<u16> {
        self.fetch_u8()
            .and_then(|l| self.fetch_u8().map(|h| combine_bytes(h, l)))
    }

    fn get_register8(&self, register: Register8) -> Result<u8> {
        match register {
            Register8::B => Ok(self.reg.b()),
            Register8::C => Ok(self.reg.c()),
            Register8::D => Ok(self.reg.d()),
            Register8::E => Ok(self.reg.e()),
            Register8::H => Ok(self.reg.h()),
            Register8::L => Ok(self.reg.l()),
            Register8::HLAsPointer => self.bus.read(self.reg.hl).map_err(|e| e.into()),
            Register8::A => Ok(self.reg.a()),
        }
    }

    fn set_register8(&mut self, register: Register8, value: u8) -> Result<()> {
        match register {
            Register8::B => {
                self.reg.set_b(value);
                Ok(())
            }
            Register8::C => {
                self.reg.set_c(value);
                Ok(())
            }
            Register8::D => {
                self.reg.set_d(value);
                Ok(())
            }
            Register8::E => {
                self.reg.set_e(value);
                Ok(())
            }
            Register8::H => {
                self.reg.set_h(value);
                Ok(())
            }
            Register8::L => {
                self.reg.set_l(value);
                Ok(())
            }
            Register8::HLAsPointer => self.bus.write(self.reg.hl, value).map_err(|e| e.into()),
            Register8::A => {
                self.reg.set_a(value);
                Ok(())
            }
        }
    }

    fn get_register16(&self, register: Register16) -> u16 {
        match register {
            Register16::BC => self.reg.bc,
            Register16::DE => self.reg.de,
            Register16::HL => self.reg.hl,
            Register16::StackPointer => self.reg.stack_ptr,
        }
    }

    fn set_register16(&mut self, register: Register16, value: u16) {
        match register {
            Register16::BC => self.reg.bc = value,
            Register16::DE => self.reg.de = value,
            Register16::HL => self.reg.hl = value,
            Register16::StackPointer => self.reg.stack_ptr = value,
        }
    }

    fn get_register16stack(&self, register: Register16Stack) -> u16 {
        match register {
            Register16Stack::BC => self.reg.bc,
            Register16Stack::DE => self.reg.de,
            Register16Stack::HL => self.reg.hl,
            Register16Stack::AF => self.reg.af,
        }
    }

    fn set_register16stack(&mut self, register: Register16Stack, value: u16) {
        match register {
            Register16Stack::BC => self.reg.bc = value,
            Register16Stack::DE => self.reg.de = value,
            Register16Stack::HL => self.reg.hl = value,
            Register16Stack::AF => self.reg.af = value,
        }
    }

    fn read_from_register16memory(&mut self, register: Register16Memory) -> Result<u8> {
        match register {
            Register16Memory::BC => self.bus.read(self.reg.bc),
            Register16Memory::DE => self.bus.read(self.reg.de),
            Register16Memory::HLInc => self
                .bus
                .read(self.reg.hl)
                .inspect(|_| self.reg.hl = self.reg.hl.wrapping_add(1)),
            Register16Memory::HLDec => self
                .bus
                .read(self.reg.hl)
                .inspect(|_| self.reg.hl = self.reg.hl.wrapping_sub(1)),
        }
        .map_err(|e| e.into())
    }

    fn write_to_register16memory(&mut self, register: Register16Memory, value: u8) -> Result<()> {
        match register {
            Register16Memory::BC => self.bus.write(self.reg.bc, value),
            Register16Memory::DE => self.bus.write(self.reg.de, value),
            Register16Memory::HLInc => self
                .bus
                .write(self.reg.hl, value)
                .inspect(|_| self.reg.hl = self.reg.hl.wrapping_add(1)),
            Register16Memory::HLDec => self
                .bus
                .write(self.reg.hl, value)
                .inspect(|_| self.reg.hl = self.reg.hl.wrapping_sub(1)),
        }
        .map_err(|e| e.into())
    }

    fn get_condition(&self, condition: Condition) -> bool {
        match condition {
            Condition::NotZero => !self.reg.zero_flag(),
            Condition::Zero => self.reg.zero_flag(),
            Condition::NotCarry => !self.reg.carry_flag(),
            Condition::Carry => self.reg.carry_flag(),
        }
    }

    fn is_overflow_bit3(old_value: i32, increment: i32) -> bool {
        ((old_value & 0x000F).wrapping_add(increment & 0x000F)) > 0x000F
    }

    fn is_overflow_bit7(old_value: i32, increment: i32) -> bool {
        ((old_value & 0x00FF).wrapping_add(increment & 0x00FF)) > 0x00FF
    }

    fn is_overflow_bit11(old_value: i32, increment: i32) -> bool {
        ((old_value & 0x0FFF).wrapping_add(increment & 0x0FFF)) > 0x0FFF
    }

    fn is_overflow_bit15(old_value: i32, increment: i32) -> bool {
        ((old_value & 0xFFFF).wrapping_add(increment & 0xFFFF)) > 0xFFFF
    }

    fn is_borroww_bit4(old_value: i32, decrement: i32) -> bool {
        ((old_value & 0x000F).wrapping_sub(decrement & 0x000F)) < 0
    }

    // Instruction execution
    fn execute(&mut self, inst: Instruction) -> Result<usize> {
        match inst {
            Instruction::Nop => Ok(Self::nop()),
            Instruction::Stop => todo!("Stop is currently not supported"),
            Instruction::DecimalAdjustA => Ok(self.decimal_adjust_a()),
            Instruction::Halt => self.halt(),
            Instruction::EnableInterrupt => Ok(self.enable_interrupt()),
            Instruction::DisableInterrupt => Ok(self.disable_interrupt()),
            Instruction::LoadLiteral8(dest) => self.load_literal8(dest),
            Instruction::LoadRegister8 { src, dest } => self.load_register8(src, dest),
            Instruction::LoadLiteral16(dest) => self.load_literal16(dest),
            Instruction::LoadFromA(dest) => self.load_from_a(dest),
            Instruction::LoadFromAIntoLiteral16Pointer => self.load_from_a_into_literal16_pointer(),
            Instruction::LoadFromAIntoLiteral8HighPointer => {
                self.load_from_a_into_literal8_high_pointer()
            }
            Instruction::LoadFromAIntoCHighPointer => self.load_from_a_into_c_high_pointer(),
            Instruction::LoadIntoA(src) => self.load_into_a(src),
            Instruction::LoadFromLiteral16PointerIntoA => self.load_from_literal16_pointer_into_a(),
            Instruction::LoadFromLiteral8HighPointerIntoA => {
                self.load_from_literal8_high_pointer_into_a()
            }
            Instruction::LoadFromCHighPointerIntoA => self.load_from_c_high_pointer_into_a(),
            Instruction::IncrementRegister8(op) => self.increment_register8(op),
            Instruction::DecrementRegister8(op) => self.decrement_register8(op),
            Instruction::AddToA(op) => self.add_to_a(op),
            Instruction::AddLiteral8ToA => self.add_literal8_to_a(),
            Instruction::AddToACarry(op) => self.add_to_a_carry(op),
            Instruction::AddLiteral8ToACarry => self.add_literal8_to_a_carry(),
            Instruction::SubtractFromA(op) => self.subtract_from_a(op),
            Instruction::SubtractLiteral8FromA => self.subtract_literal8_from_a(),
            Instruction::SubtractFromACarry(op) => self.subtract_from_a_carry(op),
            Instruction::SubtractLiteral8FromACarry => self.subtract_literal8_from_a_carry(),
            Instruction::CompareToA(op) => self.compare_to_a(op),
            Instruction::CompareLiteral8ToA => self.compare_literal8_to_a(),
            Instruction::IncrementRegister16(op) => Ok(self.increment_register16(op)),
            Instruction::DecrementRegister16(op) => Ok(self.decrement_register16(op)),
            Instruction::AddRegister16ToHL(op) => Ok(self.add_register16_to_hl(op)),
            Instruction::ComplementA => Ok(self.complement_a()),
            Instruction::AndWithA(op) => self.and_with_a(op),
            Instruction::AndLiteral8WithA => self.and_literal8_with_a(),
            Instruction::XorWithA(op) => self.xor_with_a(op),
            Instruction::XorLiteral8WithA => self.xor_literal8_with_a(),
            Instruction::OrWithA(op) => self.or_with_a(op),
            Instruction::OrLiteral8WithA => self.or_literal8_with_a(),
            Instruction::RotateLeftA => Ok(self.rotate_left_a()),
            Instruction::RotateLeftCarryA => Ok(self.rotate_left_carry_a()),
            Instruction::RotateRightA => Ok(self.rotate_right_a()),
            Instruction::RotateRightCarryA => Ok(self.rotate_right_carry_a()),
            Instruction::JumpRelative => self.jump_relative(),
            Instruction::ConditionalJumpRelative(cond) => self.conditional_jump_relative(cond),
            Instruction::Jump => self.jump(),
            Instruction::ConditionalJump(cond) => self.conditional_jump(cond),
            Instruction::JumpHL => Ok(self.jump_hl()),
            Instruction::Call => self.call(),
            Instruction::ConditionalCall(cond) => self.conditional_call(cond),
            Instruction::Restart(target) => self.restart(target),
            Instruction::Return => self.return_(),
            Instruction::ConditionalReturn(cond) => self.conditional_return(cond),
            Instruction::ReturnInterrupt => self.return_interrupt(),
            Instruction::SetCarryFlag => Ok(self.set_carry_flag()),
            Instruction::ComplementCarryFlag => Ok(self.complement_carry_flag()),
            Instruction::Push(op) => self.push(op),
            Instruction::Pop(op) => self.pop(op),
            Instruction::AddSignedLiteral8ToStackPointer => {
                self.add_signed_literal8_to_stack_pointer()
            }
            Instruction::LoadFromStackPointerIntoLiteral16Pointer => {
                self.load_from_stack_pointer_into_literal16_pointer()
            }
            Instruction::LoadFromStackPointerPlusSignedLiteral8IntoHL => {
                self.load_from_stack_pointer_plus_signed_literal8_into_hl()
            }
            Instruction::LoadFromHLIntoStackPointer => Ok(self.load_from_hl_into_stack_pointer()),
            Instruction::Prefixed => self.prefixed(),
            Instruction::Invalid(opcode) => Err(Error::InvalidInstruction { opcode }),
        }
    }

    // Misc
    fn nop() -> usize {
        1
    }

    fn decimal_adjust_a(&mut self) -> usize {
        let mut a = self.reg.a();
        let mut adjustment: u8 = 0;

        if self.reg.sub_flag() {
            if self.reg.half_carry_flag() {
                adjustment = adjustment.wrapping_add(0x06);
            }
            if self.reg.carry_flag() {
                adjustment = adjustment.wrapping_add(0x60);
            }
            a = a.wrapping_sub(adjustment);
        } else {
            if self.reg.half_carry_flag() || (a & 0x0F) > 0x09 {
                adjustment = adjustment.wrapping_add(0x06);
            }
            if self.reg.carry_flag() || a > 0x99 {
                adjustment = adjustment.wrapping_add(0x60);
                self.reg.set_carry_flag(true);
            }
            a = a.wrapping_add(adjustment);
        }

        self.reg.set_a(a);
        self.reg.set_zero_flag(a == 0);
        self.reg.set_half_carry_flag(false);

        1
    }

    // Interrupt
    fn halt(&mut self) -> Result<usize> {
        self.halted = true;

        // Halt bug
        if !self.imd.value() && self.pending_interrupts()? != 0 {
            // Halt immediatly exits on the bugged case
            self.halted = false;

            let prev_opcode = self.bus.read(self.reg.prog_counter.wrapping_sub(2))?;
            let next_opcode = self.bus.read(self.reg.prog_counter)?;

            if matches!(Instruction::from(prev_opcode), Instruction::EnableInterrupt) {
                // Interrupt will get fired and must return to the halt itself
                self.reg.prog_counter = self.reg.prog_counter.wrapping_sub(1);
            } else {
                // Regular dplication bug -> cpu immediatly wakes up and next byte gets executed twice
                self.buffered_opcode = Some(next_opcode);
            }
        }

        Ok(1)
    }

    fn enable_interrupt(&mut self) -> usize {
        self.imd.enque(true, 1); // Delay for one step
        1
    }

    fn disable_interrupt(&mut self) -> usize {
        self.imd.enque(false, 0);
        1
    }

    // Load
    fn load_literal8(&mut self, dest: Register8) -> Result<usize> {
        let value = self.fetch_u8()?;
        self.set_register8(dest, value)?;
        match dest {
            Register8::HLAsPointer => Ok(3),
            _ => Ok(2),
        }
    }
    fn load_register8(&mut self, src: Register8, dest: Register8) -> Result<usize> {
        let value = self.get_register8(src)?;
        self.set_register8(dest, value)?;
        match (src, dest) {
            (Register8::HLAsPointer, _) | (_, Register8::HLAsPointer) => Ok(2),
            _ => Ok(1),
        }
    }

    fn load_literal16(&mut self, dest: Register16) -> Result<usize> {
        let value = self.fetch_u16()?;
        self.set_register16(dest, value);
        Ok(3)
    }

    fn load_from_a(&mut self, dest: Register16Memory) -> Result<usize> {
        let value = self.reg.a();
        self.write_to_register16memory(dest, value)?;
        Ok(2)
    }

    fn load_from_a_into_literal16_pointer(&mut self) -> Result<usize> {
        let address = self.fetch_u16()?;
        self.bus.write(address, self.reg.a())?;
        Ok(4)
    }

    fn load_from_a_into_literal8_high_pointer(&mut self) -> Result<usize> {
        let address = (self.fetch_u8()? as u16).wrapping_add(0xFF00);
        self.bus.write(address, self.reg.a())?;
        Ok(3)
    }

    fn load_from_a_into_c_high_pointer(&mut self) -> Result<usize> {
        let address = (self.reg.c() as u16).wrapping_add(0xFF00);
        self.bus.write(address, self.reg.a())?;
        Ok(2)
    }

    fn load_into_a(&mut self, src: Register16Memory) -> Result<usize> {
        let value = self.read_from_register16memory(src)?;
        self.reg.set_a(value);
        Ok(2)
    }

    fn load_from_literal16_pointer_into_a(&mut self) -> Result<usize> {
        let address = self.fetch_u16()?;
        let value = self.bus.read(address)?;
        self.reg.set_a(value);
        Ok(4)
    }

    fn load_from_literal8_high_pointer_into_a(&mut self) -> Result<usize> {
        let address = (self.fetch_u8()? as u16).wrapping_add(0xFF00);
        let value = self.bus.read(address)?;
        self.reg.set_a(value);
        Ok(3)
    }

    fn load_from_c_high_pointer_into_a(&mut self) -> Result<usize> {
        let address = (self.reg.c() as u16).wrapping_add(0xFF00);
        let value = self.bus.read(address)?;
        self.reg.set_a(value);
        Ok(2)
    }

    // 8 Bit arithmetic
    fn increment_register8(&mut self, op: Register8) -> Result<usize> {
        let old_value = self.get_register8(op)?;
        let value = old_value.wrapping_add(1);
        self.set_register8(op, value)?;

        self.reg.set_zero_flag(value == 0);
        self.reg.set_sub_flag(false);
        self.reg
            .set_half_carry_flag(Self::is_overflow_bit3(old_value as i32, 1));

        match op {
            Register8::HLAsPointer => Ok(3),
            _ => Ok(1),
        }
    }

    fn decrement_register8(&mut self, op: Register8) -> Result<usize> {
        let old_value = self.get_register8(op)?;
        let value = old_value.wrapping_sub(1);
        self.set_register8(op, value)?;

        self.reg.set_zero_flag(value == 0);
        self.reg.set_sub_flag(true);
        self.reg
            .set_half_carry_flag(Self::is_borroww_bit4(old_value as i32, 1));

        match op {
            Register8::HLAsPointer => Ok(3),
            _ => Ok(1),
        }
    }

    fn add_to_a(&mut self, op: Register8) -> Result<usize> {
        let value = self.get_register8(op)?;
        self.do_add_a(value, false);
        match op {
            Register8::HLAsPointer => Ok(2),
            _ => Ok(1),
        }
    }

    fn add_literal8_to_a(&mut self) -> Result<usize> {
        let operand = self.fetch_u8()?;
        self.do_add_a(operand, false);
        Ok(2)
    }

    fn add_to_a_carry(&mut self, op: Register8) -> Result<usize> {
        let operand = self.get_register8(op)?;
        self.do_add_a(operand, self.reg.carry_flag());
        match op {
            Register8::HLAsPointer => Ok(2),
            _ => Ok(1),
        }
    }

    fn add_literal8_to_a_carry(&mut self) -> Result<usize> {
        let operand = self.fetch_u8()?;
        self.do_add_a(operand, self.reg.carry_flag());
        Ok(2)
    }

    fn do_add_a(&mut self, increment: u8, carry: bool) {
        let operand = if carry {
            increment.wrapping_add(1)
        } else {
            increment
        };

        let old_value = self.reg.a();
        let new_value = old_value.wrapping_add(operand);

        self.reg.set_a(new_value);
        self.reg.set_zero_flag(new_value == 0);
        self.reg.set_sub_flag(false);
        self.reg
            .set_half_carry_flag(Self::is_overflow_bit3(old_value as i32, operand as i32));
        self.reg
            .set_carry_flag(Self::is_overflow_bit7(old_value as i32, operand as i32));
    }

    fn subtract_from_a(&mut self, op: Register8) -> Result<usize> {
        let operand = self.get_register8(op)?;
        self.do_subtract_a(operand, false);
        match op {
            Register8::HLAsPointer => Ok(2),
            _ => Ok(1),
        }
    }

    fn subtract_literal8_from_a(&mut self) -> Result<usize> {
        let operand = self.fetch_u8()?;
        self.do_subtract_a(operand, false);
        Ok(2)
    }

    fn subtract_from_a_carry(&mut self, op: Register8) -> Result<usize> {
        let operand = self.get_register8(op)?;
        self.do_subtract_a(operand, self.reg.carry_flag());
        match op {
            Register8::HLAsPointer => Ok(2),
            _ => Ok(1),
        }
    }

    fn subtract_literal8_from_a_carry(&mut self) -> Result<usize> {
        let operand = self.fetch_u8()?;
        self.do_subtract_a(operand, self.reg.carry_flag());
        Ok(2)
    }

    fn do_subtract_a(&mut self, dec: u8, carry: bool) {
        let op = if carry { dec.wrapping_add(1) } else { dec };

        let old_value = self.reg.a();
        let new_value = old_value.wrapping_sub(op);

        self.reg.set_a(new_value);
        self.reg.set_zero_flag(new_value == 0);
        self.reg.set_sub_flag(true);
        self.reg
            .set_half_carry_flag(Self::is_borroww_bit4(old_value as i32, op as i32));
        self.reg.set_carry_flag(op > old_value);
    }

    fn compare_to_a(&mut self, op: Register8) -> Result<usize> {
        let value = self.get_register8(op)?;
        self.do_compare_a(value);
        match op {
            Register8::HLAsPointer => Ok(2),
            _ => Ok(1),
        }
    }

    fn compare_literal8_to_a(&mut self) -> Result<usize> {
        let op = self.fetch_u8()?;
        self.do_compare_a(op);
        Ok(2)
    }

    fn do_compare_a(&mut self, op: u8) {
        let a = self.reg.a();
        let check_value = a.wrapping_sub(op);

        self.reg.set_zero_flag(check_value == 0);
        self.reg.set_sub_flag(true);
        self.reg
            .set_half_carry_flag(Self::is_borroww_bit4(a as i32, op as i32));
        self.reg.set_carry_flag(op > a);
    }

    // 16 Bit arithmetic
    fn increment_register16(&mut self, op: Register16) -> usize {
        let mut value = self.get_register16(op);
        value = value.wrapping_add(1);
        self.set_register16(op, value);
        2
    }

    fn decrement_register16(&mut self, op: Register16) -> usize {
        let mut value = self.get_register16(op);
        value = value.wrapping_sub(1);
        self.set_register16(op, value);
        2
    }

    fn add_register16_to_hl(&mut self, op: Register16) -> usize {
        let old_value = self.reg.hl;
        let operand = self.get_register16(op);
        let new_value = old_value.wrapping_add(operand);

        self.reg.hl = new_value;
        self.reg.set_sub_flag(false);
        self.reg
            .set_half_carry_flag(Self::is_overflow_bit11(old_value as i32, operand as i32));
        self.reg
            .set_carry_flag(Self::is_overflow_bit15(old_value as i32, operand as i32));
        2
    }

    // Bitwise logic
    fn complement_a(&mut self) -> usize {
        self.reg.set_a(!self.reg.a());
        self.reg.set_sub_flag(true);
        self.reg.set_half_carry_flag(true);
        1
    }

    fn and_with_a(&mut self, op: Register8) -> Result<usize> {
        let operand = self.get_register8(op)?;
        self.do_and_a(operand);
        match op {
            Register8::HLAsPointer => Ok(2),
            _ => Ok(1),
        }
    }

    fn and_literal8_with_a(&mut self) -> Result<usize> {
        let operand = self.fetch_u8()?;
        self.do_and_a(operand);
        Ok(2)
    }

    fn do_and_a(&mut self, op: u8) {
        let a = self.reg.a() & op;
        self.reg.set_a(a);
        self.reg.set_zero_flag(a == 0);
        self.reg.set_sub_flag(false);
        self.reg.set_half_carry_flag(true);
        self.reg.set_carry_flag(false);
    }

    fn xor_with_a(&mut self, op: Register8) -> Result<usize> {
        let operand = self.get_register8(op)?;
        self.do_xor_a(operand);
        match op {
            Register8::HLAsPointer => Ok(2),
            _ => Ok(1),
        }
    }

    fn xor_literal8_with_a(&mut self) -> Result<usize> {
        let operand = self.fetch_u8()?;
        self.do_xor_a(operand);
        Ok(2)
    }

    fn do_xor_a(&mut self, op: u8) {
        let a = self.reg.a() ^ op;
        self.reg.set_a(a);
        self.reg.set_zero_flag(a == 0);
        self.reg.set_sub_flag(false);
        self.reg.set_half_carry_flag(false);
        self.reg.set_carry_flag(false);
    }

    fn or_with_a(&mut self, op: Register8) -> Result<usize> {
        let operand = self.get_register8(op)?;
        self.do_or_a(operand);
        match op {
            Register8::HLAsPointer => Ok(2),
            _ => Ok(1),
        }
    }

    fn or_literal8_with_a(&mut self) -> Result<usize> {
        let operand = self.fetch_u8()?;
        self.do_or_a(operand);
        Ok(2)
    }

    fn do_or_a(&mut self, op: u8) {
        let a = self.reg.a() | op;
        self.reg.set_a(a);
        self.reg.set_zero_flag(a == 0);
        self.reg.set_sub_flag(false);
        self.reg.set_half_carry_flag(false);
        self.reg.set_carry_flag(false);
    }

    // Bit shift
    fn rotate_left_a(&mut self) -> usize {
        let mut a = self.reg.a();

        let carry = a > 0b0111_1111;
        a = a.rotate_left(1);

        self.reg.set_a(a);
        self.reg.set_zero_flag(false);
        self.reg.set_sub_flag(false);
        self.reg.set_half_carry_flag(false);
        self.reg.set_carry_flag(carry);
        1
    }

    fn rotate_left_carry_a(&mut self) -> usize {
        let mut a = self.reg.a();

        let carry = a > 0b0111_1111;
        a <<= 1;
        if self.reg.carry_flag() {
            a |= 0b0000_0001;
        }

        self.reg.set_a(a);
        self.reg.set_zero_flag(false);
        self.reg.set_sub_flag(false);
        self.reg.set_half_carry_flag(false);
        self.reg.set_carry_flag(carry);
        1
    }

    fn rotate_right_a(&mut self) -> usize {
        let mut a = self.reg.a();

        let carry = (a % 2) == 1;
        a = a.rotate_right(1);

        self.reg.set_a(a);
        self.reg.set_zero_flag(false);
        self.reg.set_sub_flag(false);
        self.reg.set_half_carry_flag(false);
        self.reg.set_carry_flag(carry);
        1
    }

    fn rotate_right_carry_a(&mut self) -> usize {
        let mut a = self.reg.a();

        let carry = (a % 2) == 1;
        a >>= 1;
        if self.reg.carry_flag() {
            a |= 0b1000_0000;
        }

        self.reg.set_a(a);
        self.reg.set_zero_flag(false);
        self.reg.set_sub_flag(false);
        self.reg.set_half_carry_flag(false);
        self.reg.set_carry_flag(carry);
        1
    }

    // Jump and subroutine
    fn jump_relative(&mut self) -> Result<usize> {
        let offset = self.fetch_u8()? as i8;
        self.reg.prog_counter = self.reg.prog_counter.wrapping_add_signed(offset as i16);
        Ok(3)
    }

    fn conditional_jump_relative(&mut self, cond: Condition) -> Result<usize> {
        let condition = self.get_condition(cond);
        let offset = self.fetch_u8()? as i8;

        if condition {
            self.reg.prog_counter = self.reg.prog_counter.wrapping_add_signed(offset as i16);
            Ok(3)
        } else {
            Ok(2)
        }
    }

    fn jump(&mut self) -> Result<usize> {
        self.reg.prog_counter = self.fetch_u16()?;
        Ok(4)
    }

    fn conditional_jump(&mut self, cond: Condition) -> Result<usize> {
        let condition = self.get_condition(cond);
        let target = self.fetch_u16()?;

        if condition {
            self.reg.prog_counter = target;
            Ok(4)
        } else {
            Ok(3)
        }
    }

    fn jump_hl(&mut self) -> usize {
        self.reg.prog_counter = self.reg.hl;
        1
    }

    fn call(&mut self) -> Result<usize> {
        let address = self.fetch_u16()?;
        self.do_call(address)?;
        Ok(6)
    }

    fn conditional_call(&mut self, cond: Condition) -> Result<usize> {
        let address = self.fetch_u16()?;
        let condition = self.get_condition(cond);

        if condition {
            self.do_call(address)?;
            Ok(6)
        } else {
            Ok(3)
        }
    }

    fn restart(&mut self, target: U3) -> Result<usize> {
        let target: u8 = target.into();
        let address = (target as u16).wrapping_mul(8);
        self.do_call(address)?;
        Ok(4)
    }

    fn do_call(&mut self, address: u16) -> Result<()> {
        let ret_addr = self.reg.prog_counter;
        let high = high_byte(ret_addr);
        let low = low_byte(ret_addr);
        self.bus.write(self.reg.stack_ptr.wrapping_sub(1), high)?;
        self.bus.write(self.reg.stack_ptr.wrapping_sub(2), low)?;
        self.reg.stack_ptr = self.reg.stack_ptr.wrapping_sub(2);
        self.reg.prog_counter = address;
        Ok(())
    }

    fn return_(&mut self) -> Result<usize> {
        self.do_return()?;
        Ok(4)
    }

    fn conditional_return(&mut self, cond: Condition) -> Result<usize> {
        let condition = self.get_condition(cond);
        if condition {
            self.do_return()?;
            Ok(5)
        } else {
            Ok(2)
        }
    }

    fn return_interrupt(&mut self) -> Result<usize> {
        self.do_return()?;
        self.imd.enque(true, 0); // Is immediatly after the return
        Ok(4)
    }

    fn do_return(&mut self) -> Result<()> {
        let low = self.bus.read(self.reg.stack_ptr)?;
        let high = self.bus.read(self.reg.stack_ptr.wrapping_add(1))?;
        self.reg.stack_ptr = self.reg.stack_ptr.wrapping_add(2);
        let address = combine_bytes(high, low);
        self.reg.prog_counter = address;
        Ok(())
    }

    // Carry flag
    fn set_carry_flag(&mut self) -> usize {
        self.reg.set_sub_flag(false);
        self.reg.set_half_carry_flag(false);
        self.reg.set_carry_flag(true);
        1
    }

    fn complement_carry_flag(&mut self) -> usize {
        self.reg.set_sub_flag(false);
        self.reg.set_half_carry_flag(false);
        self.reg.set_carry_flag(!self.reg.carry_flag());
        1
    }

    // Stack manipulation
    fn push(&mut self, op: Register16Stack) -> Result<usize> {
        let value = self.get_register16stack(op);
        let high = high_byte(value);
        let low = low_byte(value);
        self.bus.write(self.reg.stack_ptr.wrapping_sub(1), high)?;
        self.bus.write(self.reg.stack_ptr.wrapping_sub(2), low)?;
        self.reg.stack_ptr = self.reg.stack_ptr.wrapping_sub(2);
        Ok(4)
    }

    fn pop(&mut self, op: Register16Stack) -> Result<usize> {
        let low = self.bus.read(self.reg.stack_ptr)?;
        let high = self.bus.read(self.reg.stack_ptr.wrapping_add(1))?;
        self.reg.stack_ptr = self.reg.stack_ptr.wrapping_add(2);
        let value = combine_bytes(high, low);
        self.set_register16stack(op, value);
        Ok(3)
    }

    fn add_signed_literal8_to_stack_pointer(&mut self) -> Result<usize> {
        let old_value = self.reg.stack_ptr;
        let operand = self.fetch_u8()? as i8;
        let new_value = old_value.wrapping_add_signed(operand as i16);

        self.reg.stack_ptr = new_value;
        self.reg.set_zero_flag(false);
        self.reg.set_sub_flag(false);
        self.reg
            .set_half_carry_flag(Self::is_overflow_bit3(old_value as i32, operand as i32));
        self.reg
            .set_carry_flag(Self::is_overflow_bit7(old_value as i32, operand as i32));
        Ok(4)
    }

    fn load_from_stack_pointer_into_literal16_pointer(&mut self) -> Result<usize> {
        let dest = self.fetch_u16()?;
        self.bus.write(dest, low_byte(self.reg.stack_ptr))?;
        self.bus
            .write(dest.wrapping_add(1), high_byte(self.reg.stack_ptr))?;
        Ok(5)
    }

    fn load_from_stack_pointer_plus_signed_literal8_into_hl(&mut self) -> Result<usize> {
        let old_value = self.reg.stack_ptr;
        let operand = self.fetch_u8()? as i8;
        let new_value = old_value.wrapping_add_signed(operand as i16);

        self.reg.hl = new_value;
        self.reg.set_zero_flag(false);
        self.reg.set_sub_flag(false);
        self.reg
            .set_half_carry_flag(Self::is_overflow_bit3(old_value as i32, operand as i32));
        self.reg
            .set_carry_flag(Self::is_overflow_bit7(old_value as i32, operand as i32));
        Ok(3)
    }

    fn load_from_hl_into_stack_pointer(&mut self) -> usize {
        self.reg.stack_ptr = self.reg.hl;
        2
    }

    // 16 Bit instructions
    fn prefixed(&mut self) -> Result<usize> {
        let next_opcode = self.fetch_u8()?;
        let inst = PrefixedInstruction::from(next_opcode);
        self.execute_prefixed(inst)
    }

    fn execute_prefixed(&mut self, inst: PrefixedInstruction) -> Result<usize> {
        match inst {
            // Bit shift
            PrefixedInstruction::RotateLeft(op) => self.prefixed_rotate_left(op),
            PrefixedInstruction::RotateLeftThroughCarry(op) => {
                self.prefixed_rotate_left_through_carry(op)
            }
            PrefixedInstruction::RotateRight(op) => self.prefixed_rotate_right(op),
            PrefixedInstruction::RotateRightThroughCarry(op) => {
                self.prefixed_rotate_right_through_carry(op)
            }
            PrefixedInstruction::ShiftLeftArithmetic(op) => self.prefixed_shift_left_arithmetic(op),
            PrefixedInstruction::ShiftRightArithmetic(op) => {
                self.prefixed_shift_right_arithmetic(op)
            }
            PrefixedInstruction::Swap(op) => self.prefixed_swap(op),
            PrefixedInstruction::ShiftRightLogical(op) => self.prefixed_shift_right_logical(op),

            // Bit flag
            PrefixedInstruction::CheckBit(op, idx) => self.prefixed_check_bit(op, idx),
            PrefixedInstruction::SetBit(op, idx) => self.prefixed_set_bit(op, idx),
            PrefixedInstruction::ResetBit(op, idx) => self.prefixed_reset_bit(op, idx),
        }
    }

    // Bit shift
    fn prefixed_rotate_left(&mut self, op: Register8) -> Result<usize> {
        let mut operand = self.get_register8(op)?;
        let carry = operand > 0b0111_1111;
        operand = operand.rotate_left(1);

        self.set_register8(op, operand)?;
        self.reg.set_zero_flag(operand == 0);
        self.reg.set_sub_flag(false);
        self.reg.set_half_carry_flag(false);
        self.reg.set_carry_flag(carry);

        match op {
            Register8::HLAsPointer => Ok(4),
            _ => Ok(2),
        }
    }

    fn prefixed_rotate_left_through_carry(&mut self, op: Register8) -> Result<usize> {
        let mut operand = self.get_register8(op)?;
        let carry = operand > 0b0111_1111;
        operand <<= 1;
        if self.reg.carry_flag() {
            operand |= 0b0000_0001;
        }

        self.set_register8(op, operand)?;
        self.reg.set_zero_flag(operand == 0);
        self.reg.set_sub_flag(false);
        self.reg.set_half_carry_flag(false);
        self.reg.set_carry_flag(carry);

        match op {
            Register8::HLAsPointer => Ok(4),
            _ => Ok(2),
        }
    }

    fn prefixed_rotate_right(&mut self, op: Register8) -> Result<usize> {
        let mut operand = self.get_register8(op)?;
        let carry = (operand % 2) == 1;
        operand = operand.rotate_right(1);

        self.set_register8(op, operand)?;
        self.reg.set_zero_flag(operand == 0);
        self.reg.set_sub_flag(false);
        self.reg.set_half_carry_flag(false);
        self.reg.set_carry_flag(carry);

        match op {
            Register8::HLAsPointer => Ok(4),
            _ => Ok(2),
        }
    }

    fn prefixed_rotate_right_through_carry(&mut self, op: Register8) -> Result<usize> {
        let mut operand = self.get_register8(op)?;
        let carry = (operand % 2) == 1;
        operand >>= 1;
        if self.reg.carry_flag() {
            operand |= 0b1000_0000;
        }

        self.set_register8(op, operand)?;
        self.reg.set_zero_flag(operand == 0);
        self.reg.set_sub_flag(false);
        self.reg.set_half_carry_flag(false);
        self.reg.set_carry_flag(carry);

        match op {
            Register8::HLAsPointer => Ok(4),
            _ => Ok(2),
        }
    }

    fn prefixed_shift_left_arithmetic(&mut self, op: Register8) -> Result<usize> {
        let mut operand = self.get_register8(op)?;
        let carry = operand > 0b0111_1111;
        operand <<= 1;

        self.set_register8(op, operand)?;
        self.reg.set_zero_flag(operand == 0);
        self.reg.set_sub_flag(false);
        self.reg.set_half_carry_flag(false);
        self.reg.set_carry_flag(carry);

        match op {
            Register8::HLAsPointer => Ok(4),
            _ => Ok(2),
        }
    }

    fn prefixed_shift_right_arithmetic(&mut self, op: Register8) -> Result<usize> {
        let mut operand = self.get_register8(op)?;
        let high_bit = operand > 0b0111_1111;
        let carry = (operand % 2) == 1;
        operand >>= 1;
        if high_bit {
            operand |= 0b1000_0000;
        }

        self.set_register8(op, operand)?;
        self.reg.set_zero_flag(operand == 0);
        self.reg.set_sub_flag(false);
        self.reg.set_half_carry_flag(false);
        self.reg.set_carry_flag(carry);

        match op {
            Register8::HLAsPointer => Ok(4),
            _ => Ok(2),
        }
    }

    fn prefixed_swap(&mut self, op: Register8) -> Result<usize> {
        let mut operand = self.get_register8(op)?;
        let lower_nibble = operand & 0xF;
        operand >>= 4;
        operand |= lower_nibble << 4;

        self.set_register8(op, operand)?;
        self.reg.set_zero_flag(operand == 0);
        self.reg.set_sub_flag(false);
        self.reg.set_half_carry_flag(false);
        self.reg.set_carry_flag(false);

        match op {
            Register8::HLAsPointer => Ok(4),
            _ => Ok(2),
        }
    }

    fn prefixed_shift_right_logical(&mut self, op: Register8) -> Result<usize> {
        let mut operand = self.get_register8(op)?;
        let carry = (operand % 2) == 1;
        operand >>= 1;

        self.set_register8(op, operand)?;
        self.reg.set_zero_flag(operand == 0);
        self.reg.set_sub_flag(false);
        self.reg.set_half_carry_flag(false);
        self.reg.set_carry_flag(carry);

        match op {
            Register8::HLAsPointer => Ok(4),
            _ => Ok(2),
        }
    }

    // Bit flag
    fn prefixed_check_bit(&mut self, op: Register8, idx: U3) -> Result<usize> {
        let operand = self.get_register8(op)?;
        let index: u8 = idx.into();

        self.reg.set_zero_flag(operand.bit(index as i32));
        self.reg.set_sub_flag(false);
        self.reg.set_half_carry_flag(true);

        match op {
            Register8::HLAsPointer => Ok(3),
            _ => Ok(2),
        }
    }

    fn prefixed_set_bit(&mut self, op: Register8, idx: U3) -> Result<usize> {
        let mut operand = self.get_register8(op)?;
        let index: u8 = idx.into();

        operand = operand.set_bit(index as i32);
        self.set_register8(op, operand)?;

        match op {
            Register8::HLAsPointer => Ok(4),
            _ => Ok(2),
        }
    }

    fn prefixed_reset_bit(&mut self, op: Register8, idx: U3) -> Result<usize> {
        let mut operand = self.get_register8(op)?;
        let index: u8 = idx.into();

        operand = operand.clear_bit(index as i32);
        self.set_register8(op, operand)?;

        match op {
            Register8::HLAsPointer => Ok(4),
            _ => Ok(2),
        }
    }
}

#[cfg(test)]
mod tests;
