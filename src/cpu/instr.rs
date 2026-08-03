use crate::bits::{BitPattern, Bits};

const O: bool = false;
const I: bool = true;

pub enum U3 {
    Zero,
    One,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
}

fn u3(pattern: BitPattern<3>) -> U3 {
    match pattern {
        [O, O, O] => U3::Zero,
        [O, O, I] => U3::One,
        [O, I, O] => U3::Two,
        [O, I, I] => U3::Three,
        [I, O, O] => U3::Four,
        [I, O, I] => U3::Five,
        [I, I, O] => U3::Six,
        [I, I, I] => U3::Seven,
    }
}

impl From<U3> for u8 {
    fn from(val: U3) -> Self {
        match val {
            U3::Zero => 0,
            U3::One => 1,
            U3::Two => 2,
            U3::Three => 3,
            U3::Four => 4,
            U3::Five => 5,
            U3::Six => 6,
            U3::Seven => 7,
        }
    }
}

#[derive(Clone, Copy)]
pub enum Register8 {
    B,
    C,
    D,
    E,
    H,
    L,
    HLAsPointer,
    A,
}

fn r8(pattern: BitPattern<3>) -> Register8 {
    match pattern {
        [O, O, O] => Register8::B,
        [O, O, I] => Register8::C,
        [O, I, O] => Register8::D,
        [O, I, I] => Register8::E,
        [I, O, O] => Register8::H,
        [I, O, I] => Register8::L,
        [I, I, O] => Register8::HLAsPointer,
        [I, I, I] => Register8::A,
    }
}

#[derive(Clone, Copy)]
pub enum Register16 {
    BC,
    DE,
    HL,
    StackPointer,
}

fn r16(pattern: BitPattern<2>) -> Register16 {
    match pattern {
        [O, O] => Register16::BC,
        [O, I] => Register16::DE,
        [I, O] => Register16::HL,
        [I, I] => Register16::StackPointer,
    }
}

pub enum Register16Stack {
    BC,
    DE,
    HL,
    AF,
}

fn r16stk(pattern: BitPattern<2>) -> Register16Stack {
    match pattern {
        [O, O] => Register16Stack::BC,
        [O, I] => Register16Stack::DE,
        [I, O] => Register16Stack::HL,
        [I, I] => Register16Stack::AF,
    }
}

pub enum Register16Memory {
    BC,
    DE,
    HLInc,
    HLDec,
}

fn r16mem(value: BitPattern<2>) -> Register16Memory {
    match value {
        [O, O] => Register16Memory::BC,
        [O, I] => Register16Memory::DE,
        [I, O] => Register16Memory::HLInc,
        [I, I] => Register16Memory::HLDec,
    }
}

pub enum Condition {
    NotZero,
    Zero,
    NotCarry,
    Carry,
}

fn cond(pattern: BitPattern<2>) -> Condition {
    match pattern {
        [O, O] => Condition::NotZero,
        [O, I] => Condition::Zero,
        [I, O] => Condition::NotCarry,
        [I, I] => Condition::Carry,
    }
}

pub enum Instruction {
    // Misc
    Nop,
    Stop,
    DecimalAdjustA,

    // Interrupt
    Halt,
    EnableInterrupt,
    DisableInterrupt,

    // Load
    LoadLiteral8(Register8),
    LoadRegister8 { src: Register8, dest: Register8 },
    LoadLiteral16(Register16),
    LoadFromA(Register16Memory),
    LoadFromAIntoLiteral16Pointer,
    LoadFromAIntoLiteral8HighPointer,
    LoadFromAIntoCHighPointer,
    LoadIntoA(Register16Memory),
    LoadFromLiteral16PointerIntoA,
    LoadFromLiteral8HighPointerIntoA,
    LoadFromCHighPointerIntoA,

    // 8 Bit arithmetic
    IncrementRegister8(Register8),
    DecrementRegister8(Register8),
    AddToA(Register8),
    AddLiteral8ToA,
    AddToACarry(Register8),
    AddLiteral8ToACarry,
    SubtractFromA(Register8),
    SubtractLiteral8FromA,
    SubtractFromACarry(Register8),
    SubtractLiteral8FromACarry,
    CompareToA(Register8),
    CompareLiteral8ToA,

    // 16 Bit arithmetic
    IncrementRegister16(Register16),
    DecrementRegister16(Register16),
    AddRegister16ToHL(Register16),

    // Bitwise logic
    ComplementA,
    AndWithA(Register8),
    AndLiteral8WithA,
    XorWithA(Register8),
    XorLiteral8WithA,
    OrWithA(Register8),
    OrLiteral8WithA,

    // Bit shift
    RotateLeftA,
    RotateLeftCarryA,
    RotateRightA,
    RotateRightCarryA,

    // Jump and subroutine
    JumpRelative,
    ConditionalJumpRelative(Condition),
    Jump,
    ConditionalJump(Condition),
    JumpHL,
    Call,
    ConditionalCall(Condition),
    Restart(U3),
    Return,
    ConditionalReturn(Condition),
    ReturnInterrupt,

    // Carry flag
    SetCarryFlag,
    ComplementCarryFlag,

    // Stack manipulation
    Push(Register16Stack),
    Pop(Register16Stack),
    AddSignedLiteral8ToStackPointer,
    LoadFromStackPointerIntoLiteral16Pointer,
    LoadFromStackPointerPlusSignedLiteral8IntoHL,
    LoadFromHLIntoStackPointer,

    // 16 Bit instructions
    Prefixed,

    Invalid(u8),
}

impl Instruction {
    pub fn from(opcode: u8) -> Self {
        match opcode.bits_msb_first() {
            // Misc
            [O, O, O, O, O, O, O, O] => Self::Nop,
            [O, O, O, I, O, O, O, O] => Self::Stop,
            [O, O, I, O, O, I, I, I] => Self::DecimalAdjustA,

            // Interrupt
            [O, I, I, I, O, I, I, O] => Self::Halt,
            [I, I, I, I, I, O, I, I] => Self::EnableInterrupt,
            [I, I, I, I, O, O, I, I] => Self::DisableInterrupt,

            // Load
            [O, O, a1, a2, a3, I, I, O] => Self::LoadLiteral8(r8([a1, a2, a3])),
            [O, I, a1, a2, a3, b1, b2, b3] => Self::LoadRegister8 {
                src: r8([b1, b2, b3]),
                dest: r8([a1, a2, a3]),
            }, // Must be after halt, halt has same bit pattern as ld [hl], [hl]
            [O, O, a1, a2, O, O, O, I] => Self::LoadLiteral16(r16([a1, a2])),
            [O, O, a1, a2, O, O, I, O] => Self::LoadFromA(r16mem([a1, a2])),
            [I, I, I, O, I, O, I, O] => Self::LoadFromAIntoLiteral16Pointer,
            [I, I, I, O, O, O, O, O] => Self::LoadFromAIntoLiteral8HighPointer,
            [I, I, I, O, O, O, I, O] => Self::LoadFromAIntoCHighPointer,
            [O, O, a1, a2, I, O, I, O] => Self::LoadIntoA(r16mem([a1, a2])),
            [I, I, I, I, I, O, I, O] => Self::LoadFromLiteral16PointerIntoA,
            [I, I, I, I, O, O, O, O] => Self::LoadFromLiteral8HighPointerIntoA,
            [I, I, I, I, O, O, I, O] => Self::LoadFromCHighPointerIntoA,

            // 8 Bit arithmetic
            [O, O, a1, a2, a3, I, O, O] => Self::IncrementRegister8(r8([a1, a2, a3])),
            [O, O, a1, a2, a3, I, O, I] => Self::DecrementRegister8(r8([a1, a2, a3])),
            [I, O, O, O, O, a1, a2, a3] => Self::AddToA(r8([a1, a2, a3])),
            [I, I, O, O, O, I, I, O] => Self::AddLiteral8ToA,
            [I, O, O, O, I, a1, a2, a3] => Self::AddToACarry(r8([a1, a2, a3])),
            [I, I, O, O, I, I, I, O] => Self::AddLiteral8ToACarry,
            [I, O, O, I, O, a1, a2, a3] => Self::SubtractFromA(r8([a1, a2, a3])),
            [I, I, O, I, O, I, I, O] => Self::SubtractLiteral8FromA,
            [I, O, O, I, I, a1, a2, a3] => Self::SubtractFromACarry(r8([a1, a2, a3])),
            [I, I, O, I, I, I, I, O] => Self::SubtractLiteral8FromACarry,
            [I, O, I, I, I, a1, a2, a3] => Self::CompareToA(r8([a1, a2, a3])),
            [I, I, I, I, I, I, I, O] => Self::CompareLiteral8ToA,

            // 16 Bit arithmetic
            [O, O, a1, a2, O, O, I, I] => Self::IncrementRegister16(r16([a1, a2])),
            [O, O, a1, a2, I, O, I, I] => Self::DecrementRegister16(r16([a1, a2])),
            [O, O, a1, a2, I, O, O, I] => Self::AddRegister16ToHL(r16([a1, a2])),

            // Bitwise logic
            [O, O, I, O, I, I, I, I] => Self::ComplementA,
            [I, O, I, O, O, a1, a2, a3] => Self::AndWithA(r8([a1, a2, a3])), // TODO Check
            [I, I, I, O, O, I, I, O] => Self::AndLiteral8WithA,
            [I, O, I, O, I, a1, a2, a3] => Self::XorWithA(r8([a1, a2, a3])),
            [I, I, I, O, I, I, I, O] => Self::XorLiteral8WithA,
            [I, O, I, I, O, a1, a2, a3] => Self::OrWithA(r8([a1, a2, a3])),
            [I, I, I, I, O, I, I, O] => Self::OrLiteral8WithA,

            // Bit shift
            [O, O, O, O, O, I, I, I] => Self::RotateLeftA,
            [O, O, O, I, O, I, I, I] => Self::RotateLeftCarryA,
            [O, O, O, O, I, I, I, I] => Self::RotateRightA,
            [O, O, O, I, I, I, I, I] => Self::RotateRightCarryA,

            // Jump and subroutine
            [O, O, O, I, I, O, O, O] => Self::JumpRelative,
            [O, O, I, a1, a2, O, O, O] => Self::ConditionalJumpRelative(cond([a1, a2])),
            [I, I, O, O, O, O, I, I] => Self::Jump,
            [I, I, O, a1, a2, O, I, O] => Self::ConditionalJump(cond([a1, a2])),
            [I, I, I, O, I, O, O, I] => Self::JumpHL,
            [I, I, O, O, I, I, O, I] => Self::Call,
            [I, I, O, a1, a2, I, O, O] => Self::ConditionalCall(cond([a1, a2])),
            [I, I, a1, a2, a3, I, I, I] => Self::Restart(u3([a1, a2, a3])),
            [I, I, O, O, I, O, O, I] => Self::Return,
            [I, I, O, a1, a2, O, O, O] => Self::ConditionalReturn(cond([a1, a2])),
            [I, I, O, I, I, O, O, I] => Self::ReturnInterrupt,

            // Carry flag
            [O, O, I, I, O, I, I, I] => Self::SetCarryFlag,
            [O, O, I, I, I, I, I, I] => Self::ComplementCarryFlag,

            // Stack manipulation
            [I, I, a1, a2, O, I, O, I] => Self::Push(r16stk([a1, a2])),
            [I, I, a1, a2, O, O, O, I] => Self::Pop(r16stk([a1, a2])),
            [I, I, I, O, I, O, O, O] => Self::AddSignedLiteral8ToStackPointer,
            [O, O, O, O, I, O, O, O] => Self::LoadFromStackPointerIntoLiteral16Pointer,
            [I, I, I, I, I, O, O, O] => Self::LoadFromStackPointerPlusSignedLiteral8IntoHL,
            [I, I, I, I, I, O, O, I] => Self::LoadFromHLIntoStackPointer,

            // 16 Bit instructions
            [I, I, O, O, I, O, I, I] => Self::Prefixed,

            // Invalid opcodes
            [I, I, O, I, O, O, I, I] => Self::Invalid(opcode),
            [I, I, I, O, O, O, I, I] => Self::Invalid(opcode),
            [I, I, I, O, O, I, O, O] => Self::Invalid(opcode),
            [I, I, I, I, O, I, O, O] => Self::Invalid(opcode),
            [I, I, O, I, I, O, I, I] => Self::Invalid(opcode),
            [I, I, I, O, I, O, I, I] => Self::Invalid(opcode),
            [I, I, I, O, I, I, O, O] => Self::Invalid(opcode),
            [I, I, I, I, I, I, O, O] => Self::Invalid(opcode),
            [I, I, O, I, I, I, O, I] => Self::Invalid(opcode),
            [I, I, I, O, I, I, O, I] => Self::Invalid(opcode),
            [I, I, I, I, I, I, O, I] => Self::Invalid(opcode),
        }
    }
}

pub enum PrefixedInstruction {
    // Bit shift
    RotateLeft(Register8),
    RotateLeftThroughCarry(Register8),
    RotateRight(Register8),
    RotateRightThroughCarry(Register8),
    ShiftLeftArithmetic(Register8),
    ShiftRightArithmetic(Register8),
    Swap(Register8),
    ShiftRightLogical(Register8),

    // Bit flag
    CheckBit(Register8, U3),
    SetBit(Register8, U3),
    ResetBit(Register8, U3),
}

impl PrefixedInstruction {
    pub fn from(opcode: u8) -> Self {
        match opcode.bits_msb_first() {
            [O, O, O, O, O, x1, x2, x3] => Self::RotateLeft(r8([x1, x2, x3])),
            [O, O, O, O, I, x1, x2, x3] => Self::RotateRight(r8([x1, x2, x3])),
            [O, O, O, I, O, x1, x2, x3] => Self::RotateLeftThroughCarry(r8([x1, x2, x3])),
            [O, O, O, I, I, x1, x2, x3] => Self::RotateRightThroughCarry(r8([x1, x2, x3])),
            [O, O, I, O, O, x1, x2, x3] => Self::ShiftLeftArithmetic(r8([x1, x2, x3])),
            [O, O, I, O, I, x1, x2, x3] => Self::ShiftRightArithmetic(r8([x1, x2, x3])),
            [O, O, I, I, O, x1, x2, x3] => Self::Swap(r8([x1, x2, x3])),
            [O, O, I, I, I, x1, x2, x3] => Self::ShiftRightLogical(r8([x1, x2, x3])),
            [O, I, x1, x2, x3, y1, y2, y3] => Self::CheckBit(r8([y1, y2, y3]), u3([x1, x2, x3])),
            [I, O, x1, x2, x3, y1, y2, y3] => Self::ResetBit(r8([y1, y2, y3]), u3([x1, x2, x3])),
            [I, I, x1, x2, x3, y1, y2, y3] => Self::SetBit(r8([y1, y2, y3]), u3([x1, x2, x3])),
        }
    }
}
