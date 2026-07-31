#[derive(Copy, Clone)]
pub enum Register8 {
    B = 0,
    C = 1,
    D = 2,
    E = 3,
    H = 4,
    L = 5,
    HLAsPointer = 6,
    A = 7,
}

#[derive(Copy, Clone)]
pub enum Register16 {
    BC = 0,
    DE = 1,
    HL = 2,
    StackPointer = 3,
}

#[derive(Copy, Clone)]
pub enum Register16Stack {
    BC = 0,
    DE = 1,
    HL = 2,
    AF = 3,
}

#[derive(Copy, Clone)]
pub enum Register16Memory {
    BC = 0,
    DE = 1,
    HLInc = 2,
    HLDec = 3,
}

#[derive(Copy, Clone)]
pub enum Condition {
    NotZero = 0,
    Zero = 1,
    NotCarry = 2,
    Carry = 3,
}

#[derive(Copy, Clone)]
pub enum U3 {
    Zero = 0,
    One = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
}

#[inline]
fn slice(x: u8, offset: u8, length: u8) -> u8 {
    let mask = !((0xFF as u8) << length);
    (x >> offset) & mask
}

#[inline]
pub fn r8(x: u8, offset: u8) -> Register8 {
    match slice(x, offset, 3) {
        0 => Register8::B,
        1 => Register8::C,
        2 => Register8::D,
        3 => Register8::E,
        4 => Register8::H,
        5 => Register8::L,
        6 => Register8::HLAsPointer,
        7 => Register8::A,
        _ => unreachable!(),
    }
}

#[inline]
pub fn r16(x: u8, offset: u8) -> Register16 {
    match slice(x, offset, 2) {
        0 => Register16::BC,
        1 => Register16::DE,
        2 => Register16::HL,
        3 => Register16::StackPointer,
        _ => unreachable!(),
    }
}

#[inline]
pub fn r16stk(x: u8, offset: u8) -> Register16Stack {
    match slice(x, offset, 2) {
        0 => Register16Stack::BC,
        1 => Register16Stack::DE,
        2 => Register16Stack::HL,
        3 => Register16Stack::AF,
        _ => unreachable!(),
    }
}

#[inline]
pub fn r16mem(x: u8, offset: u8) -> Register16Memory {
    match slice(x, offset, 2) {
        0 => Register16Memory::BC,
        1 => Register16Memory::DE,
        2 => Register16Memory::HLInc,
        3 => Register16Memory::HLDec,
        _ => unreachable!(),
    }
}

#[inline]
pub fn cond(x: u8, offset: u8) -> Condition {
    match slice(x, offset, 2) {
        0 => Condition::NotZero,
        1 => Condition::Zero,
        2 => Condition::NotCarry,
        3 => Condition::Carry,
        _ => unreachable!(),
    }
}

#[inline]
pub fn u3(x: u8, offset: u8) -> U3 {
    match slice(x, offset, 2) {
        0 => U3::Zero,
        1 => U3::One,
        2 => U3::Two,
        3 => U3::Three,
        4 => U3::Four,
        5 => U3::Five,
        6 => U3::Six,
        7 => U3::Seven,
        _ => unreachable!(),
    }
}

impl U3 {
    #[inline]
    pub fn to_u8(self) -> u8 {
        match self {
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
}

impl Instruction {
    pub fn from_opcode(opcode: u8) -> Option<Self> {
        let inst = match opcode {
            // Misc
            0x00 => Self::Nop,
            0b0001_0000 => Self::Stop,
            0b0010_0111 => Self::DecimalAdjustA,

            // Interrupt
            0b0111_0110 => Self::Halt,
            0b1111_1011 => Self::EnableInterrupt,
            0b1111_0011 => Self::DisableInterrupt,

            // Load
            x if (x & 0b1100_0111) == 0b0000_0110 => Self::LoadLiteral8(r8(x, 3)),
            x if (x & 0b1100_0000) == 0b0100_0000 => Self::LoadRegister8 {
                src: r8(x, 0),
                dest: r8(x, 3),
            }, // Must be after halt, halt has same bit pattern as ld [hl], [hl]
            x if (x & 0b1100_1111) == 0b0000_0001 => Self::LoadLiteral16(r16(x, 4)),
            x if (x & 0b1100_1111) == 0b0000_0010 => Self::LoadFromA(r16mem(x, 4)),
            0b1110_1010 => Self::LoadFromAIntoLiteral16Pointer,
            0b1110_0000 => Self::LoadFromAIntoLiteral8HighPointer,
            0b1110_0010 => Self::LoadFromAIntoCHighPointer,
            x if (x & 0b1100_1111) == 0b0000_1010 => Self::LoadIntoA(r16mem(x, 4)),
            0b1111_1010 => Self::LoadFromLiteral16PointerIntoA,
            0b1111_0000 => Self::LoadFromLiteral8HighPointerIntoA,
            0b1111_0010 => Self::LoadFromCHighPointerIntoA,

            // 8 Bit arithmetic
            x if (x & 0b1100_0111) == 0b0000_0100 => Self::IncrementRegister8(r8(x, 3)),
            x if (x & 0b1100_0111) == 0b0000_0101 => Self::DecrementRegister8(r8(x, 3)),
            x if (x & 0b1111_1000) == 0b1000_0000 => Self::AddToA(r8(x, 0)),
            0b1100_0110 => Self::AddLiteral8ToA,
            x if (x & 0b1111_1000) == 0b1000_1000 => Self::AddToACarry(r8(x, 0)),
            0b1100_1110 => Self::AddLiteral8ToACarry,
            x if (x & 0b1111_1000) == 0b1001_0000 => Self::SubtractFromA(r8(x, 0)),
            0b1101_0110 => Self::SubtractLiteral8FromA,
            x if (x & 0b1111_1000) == 0b1001_1000 => Self::SubtractFromACarry(r8(x, 0)),
            0b1101_1110 => Self::SubtractLiteral8FromACarry,
            x if (x & 0b1111_1000) == 0b1011_1000 => Self::CompareToA(r8(x, 0)),
            0b1111_1110 => Self::CompareLiteral8ToA,

            // 16 Bit arithmetic
            x if (x & 0b1100_1111) == 0b0000_0011 => Self::IncrementRegister16(r16(x, 4)),
            x if (x & 0b1100_1111) == 0b0000_1011 => Self::DecrementRegister16(r16(x, 4)),
            x if (x & 0b1100_1111) == 0b0000_1001 => Self::AddRegister16ToHL(r16(x, 4)),

            // Bitwise logic
            0b0010_1111 => Self::ComplementA,
            x if (x & 0b1111_1000) == 0b1010_0000 => Self::AndWithA(r8(x, 0)),
            0b1110_0110 => Self::AndLiteral8WithA,
            x if (x & 0b1111_1000) == 0b1010_1000 => Self::XorWithA(r8(x, 0)),
            0b1110_1110 => Self::XorLiteral8WithA,
            x if (x & 0b1111_1000) == 0b1011_0000 => Self::OrWithA(r8(x, 0)),
            0b1111_0110 => Self::OrLiteral8WithA,

            // Bit shift
            0b0000_0111 => Self::RotateLeftA,
            0b0001_0111 => Self::RotateLeftCarryA,
            0b0000_1111 => Self::RotateRightA,
            0b0001_1111 => Self::RotateRightCarryA,

            // Jump and subroutine
            0b0001_1000 => Self::JumpRelative,
            x if (x & 0b1110_0111) == 0b0010_0000 => Self::ConditionalJumpRelative(cond(x, 3)),
            0b1100_0011 => Self::Jump,
            x if (x & 0b1110_0111) == 0b1100_0010 => Self::ConditionalJump(cond(x, 3)),
            0b1110_1001 => Self::JumpHL,
            0b1100_1101 => Self::Call,
            x if (x & 0b1110_0111) == 0b1100_0100 => Self::ConditionalCall(cond(x, 3)),
            x if (x & 0b1100_0111) == 0b1100_0111 => Self::Restart(u3(x, 3)),
            0b1100_1001 => Self::Return,
            x if (x & 0b1110_0111) == 0b1100_0000 => Self::ConditionalReturn(cond(x, 3)),
            0b1101_1001 => Self::ReturnInterrupt,

            // Carry flag
            0b0011_0111 => Self::SetCarryFlag,
            0b0011_1111 => Self::ComplementCarryFlag,

            // Stack manipulation
            x if (x & 0b1100_1111) == 0b1100_0101 => Self::Push(r16stk(x, 4)),
            x if (x & 0b1100_1111) == 0b1100_0001 => Self::Pop(r16stk(x, 4)),
            0b1110_1000 => Self::AddSignedLiteral8ToStackPointer,
            0b0000_1000 => Self::LoadFromStackPointerIntoLiteral16Pointer,
            0b1111_1000 => Self::LoadFromStackPointerPlusSignedLiteral8IntoHL,
            0b1111_1001 => Self::LoadFromHLIntoStackPointer,

            // 16 Bit instructions
            0xCB => Self::Prefixed,

            _ => None?,
        };
        Some(inst)
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
    pub fn from_opcode(opcode: u8) -> Self {
        match opcode {
            x if (x & 0b1111_1000) == 0b0000_0000 => Self::RotateLeft(r8(x, 0)),
            x if (x & 0b1111_1000) == 0b0000_1000 => Self::RotateRight(r8(x, 0)),
            x if (x & 0b1111_1000) == 0b0001_0000 => Self::RotateLeftThroughCarry(r8(x, 0)),
            x if (x & 0b1111_1000) == 0b0001_1000 => Self::RotateRightThroughCarry(r8(x, 0)),
            x if (x & 0b1111_1000) == 0b0010_0000 => Self::ShiftLeftArithmetic(r8(x, 0)),
            x if (x & 0b1111_1000) == 0b0010_1000 => Self::ShiftRightArithmetic(r8(x, 0)),
            x if (x & 0b1111_1000) == 0b0011_0000 => Self::Swap(r8(x, 0)),
            x if (x & 0b1111_1000) == 0b0011_1000 => Self::ShiftRightLogical(r8(x, 0)),
            x if (x & 0b1100_0000) == 0b0100_0000 => Self::CheckBit(r8(x, 0), u3(x, 3)),
            x if (x & 0b1100_0000) == 0b1000_0000 => Self::ResetBit(r8(x, 0), u3(x, 3)),
            x if (x & 0b1100_0000) == 0b1100_0000 => Self::SetBit(r8(x, 0), u3(x, 3)),
            _ => unreachable!(),
        }
    }
}
