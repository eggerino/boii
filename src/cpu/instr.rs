enum U2 {
    Zero,
    One,
    Two,
    Three,
}

impl U2 {
    fn from(x: u8, offset: u8) -> Self {
        let mask = 0b11;
        match (x >> offset) & mask {
            0 => Self::Zero,
            1 => Self::One,
            2 => Self::Two,
            3 => Self::Three,
            _ => unreachable!(),
        }
    }
}

#[derive(Copy, Clone)]
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

impl U3 {
    fn from(x: u8, offset: u8) -> Self {
        let mask = 0b111;
        match (x >> offset) & mask {
            0 => Self::Zero,
            1 => Self::One,
            2 => Self::Two,
            3 => Self::Three,
            4 => Self::Four,
            5 => Self::Five,
            6 => Self::Six,
            7 => Self::Seven,
            _ => unreachable!(),
        }
    }
}

impl Into<u8> for U3 {
    fn into(self) -> u8 {
        match self {
            Self::Zero => 0,
            Self::One => 1,
            Self::Two => 2,
            Self::Three => 3,
            Self::Four => 4,
            Self::Five => 5,
            Self::Six => 6,
            Self::Seven => 7,
        }
    }
}

#[derive(Copy, Clone)]
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

impl Register8 {
    fn from(x: u8, offset: u8) -> Self {
        match U3::from(x, offset) {
            U3::Zero => Self::B,
            U3::One => Self::C,
            U3::Two => Self::D,
            U3::Three => Self::E,
            U3::Four => Self::H,
            U3::Five => Self::L,
            U3::Six => Self::HLAsPointer,
            U3::Seven => Self::A,
        }
    }
}

#[derive(Copy, Clone)]
pub enum Register16 {
    BC,
    DE,
    HL,
    StackPointer,
}

impl Register16 {
    fn from(x: u8, offset: u8) -> Self {
        match U2::from(x, offset) {
            U2::Zero => Self::BC,
            U2::One => Self::DE,
            U2::Two => Self::HL,
            U2::Three => Self::StackPointer,
        }
    }
}

#[derive(Copy, Clone)]
pub enum Register16Stack {
    BC,
    DE,
    HL,
    AF,
}

impl Register16Stack {
    fn from(x: u8, offset: u8) -> Self {
        match U2::from(x, offset) {
            U2::Zero => Self::BC,
            U2::One => Self::DE,
            U2::Two => Self::HL,
            U2::Three => Self::AF,
        }
    }
}

#[derive(Copy, Clone)]
pub enum Register16Memory {
    BC,
    DE,
    HLInc,
    HLDec,
}

impl Register16Memory {
    fn from(x: u8, offset: u8) -> Self {
        match U2::from(x, offset) {
            U2::Zero => Self::BC,
            U2::One => Self::DE,
            U2::Two => Self::HLInc,
            U2::Three => Self::HLDec,
        }
    }
}

#[derive(Copy, Clone)]
pub enum Condition {
    NotZero,
    Zero,
    NotCarry,
    Carry,
}

impl Condition {
    fn from(x: u8, offset: u8) -> Self {
        match U2::from(x, offset) {
            U2::Zero => Self::NotZero,
            U2::One => Self::Zero,
            U2::Two => Self::NotCarry,
            U2::Three => Self::Carry,
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

    Invalid,
}

impl Instruction {
    pub fn from(opcode: u8) -> Self {
        match opcode {
            // Misc
            0x00 => Self::Nop,
            0b0001_0000 => Self::Stop,
            0b0010_0111 => Self::DecimalAdjustA,

            // Interrupt
            0b0111_0110 => Self::Halt,
            0b1111_1011 => Self::EnableInterrupt,
            0b1111_0011 => Self::DisableInterrupt,

            // Load
            x if (x & 0b1100_0111) == 0b0000_0110 => Self::LoadLiteral8(Register8::from(x, 3)),
            x if (x & 0b1100_0000) == 0b0100_0000 => Self::LoadRegister8 {
                src: Register8::from(x, 0),
                dest: Register8::from(x, 3),
            }, // Must be after halt, halt has same bit pattern as ld [hl], [hl]
            x if (x & 0b1100_1111) == 0b0000_0001 => Self::LoadLiteral16(Register16::from(x, 4)),
            x if (x & 0b1100_1111) == 0b0000_0010 => Self::LoadFromA(Register16Memory::from(x, 4)),
            0b1110_1010 => Self::LoadFromAIntoLiteral16Pointer,
            0b1110_0000 => Self::LoadFromAIntoLiteral8HighPointer,
            0b1110_0010 => Self::LoadFromAIntoCHighPointer,
            x if (x & 0b1100_1111) == 0b0000_1010 => Self::LoadIntoA(Register16Memory::from(x, 4)),
            0b1111_1010 => Self::LoadFromLiteral16PointerIntoA,
            0b1111_0000 => Self::LoadFromLiteral8HighPointerIntoA,
            0b1111_0010 => Self::LoadFromCHighPointerIntoA,

            // 8 Bit arithmetic
            x if (x & 0b1100_0111) == 0b0000_0100 => {
                Self::IncrementRegister8(Register8::from(x, 3))
            }
            x if (x & 0b1100_0111) == 0b0000_0101 => {
                Self::DecrementRegister8(Register8::from(x, 3))
            }
            x if (x & 0b1111_1000) == 0b1000_0000 => Self::AddToA(Register8::from(x, 0)),
            0b1100_0110 => Self::AddLiteral8ToA,
            x if (x & 0b1111_1000) == 0b1000_1000 => Self::AddToACarry(Register8::from(x, 0)),
            0b1100_1110 => Self::AddLiteral8ToACarry,
            x if (x & 0b1111_1000) == 0b1001_0000 => Self::SubtractFromA(Register8::from(x, 0)),
            0b1101_0110 => Self::SubtractLiteral8FromA,
            x if (x & 0b1111_1000) == 0b1001_1000 => {
                Self::SubtractFromACarry(Register8::from(x, 0))
            }
            0b1101_1110 => Self::SubtractLiteral8FromACarry,
            x if (x & 0b1111_1000) == 0b1011_1000 => Self::CompareToA(Register8::from(x, 0)),
            0b1111_1110 => Self::CompareLiteral8ToA,

            // 16 Bit arithmetic
            x if (x & 0b1100_1111) == 0b0000_0011 => {
                Self::IncrementRegister16(Register16::from(x, 4))
            }
            x if (x & 0b1100_1111) == 0b0000_1011 => {
                Self::DecrementRegister16(Register16::from(x, 4))
            }
            x if (x & 0b1100_1111) == 0b0000_1001 => {
                Self::AddRegister16ToHL(Register16::from(x, 4))
            }

            // Bitwise logic
            0b0010_1111 => Self::ComplementA,
            x if (x & 0b1111_1000) == 0b1010_0000 => Self::AndWithA(Register8::from(x, 0)),
            0b1110_0110 => Self::AndLiteral8WithA,
            x if (x & 0b1111_1000) == 0b1010_1000 => Self::XorWithA(Register8::from(x, 0)),
            0b1110_1110 => Self::XorLiteral8WithA,
            x if (x & 0b1111_1000) == 0b1011_0000 => Self::OrWithA(Register8::from(x, 0)),
            0b1111_0110 => Self::OrLiteral8WithA,

            // Bit shift
            0b0000_0111 => Self::RotateLeftA,
            0b0001_0111 => Self::RotateLeftCarryA,
            0b0000_1111 => Self::RotateRightA,
            0b0001_1111 => Self::RotateRightCarryA,

            // Jump and subroutine
            0b0001_1000 => Self::JumpRelative,
            x if (x & 0b1110_0111) == 0b0010_0000 => {
                Self::ConditionalJumpRelative(Condition::from(x, 3))
            }
            0b1100_0011 => Self::Jump,
            x if (x & 0b1110_0111) == 0b1100_0010 => Self::ConditionalJump(Condition::from(x, 3)),
            0b1110_1001 => Self::JumpHL,
            0b1100_1101 => Self::Call,
            x if (x & 0b1110_0111) == 0b1100_0100 => Self::ConditionalCall(Condition::from(x, 3)),
            x if (x & 0b1100_0111) == 0b1100_0111 => Self::Restart(U3::from(x, 3)),
            0b1100_1001 => Self::Return,
            x if (x & 0b1110_0111) == 0b1100_0000 => Self::ConditionalReturn(Condition::from(x, 3)),
            0b1101_1001 => Self::ReturnInterrupt,

            // Carry flag
            0b0011_0111 => Self::SetCarryFlag,
            0b0011_1111 => Self::ComplementCarryFlag,

            // Stack manipulation
            x if (x & 0b1100_1111) == 0b1100_0101 => Self::Push(Register16Stack::from(x, 4)),
            x if (x & 0b1100_1111) == 0b1100_0001 => Self::Pop(Register16Stack::from(x, 4)),
            0b1110_1000 => Self::AddSignedLiteral8ToStackPointer,
            0b0000_1000 => Self::LoadFromStackPointerIntoLiteral16Pointer,
            0b1111_1000 => Self::LoadFromStackPointerPlusSignedLiteral8IntoHL,
            0b1111_1001 => Self::LoadFromHLIntoStackPointer,

            // 16 Bit instructions
            0xCB => Self::Prefixed,

            0xD3 | 0xE3 | 0xE4 | 0xF4 | 0xDB | 0xEB | 0xEC | 0xFC | 0xDD | 0xED | 0xFD => {
                Self::Invalid
            }

            _ => unreachable!(),
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
        match opcode {
            x if (x & 0b1111_1000) == 0b0000_0000 => Self::RotateLeft(Register8::from(x, 0)),
            x if (x & 0b1111_1000) == 0b0000_1000 => Self::RotateRight(Register8::from(x, 0)),
            x if (x & 0b1111_1000) == 0b0001_0000 => {
                Self::RotateLeftThroughCarry(Register8::from(x, 0))
            }
            x if (x & 0b1111_1000) == 0b0001_1000 => {
                Self::RotateRightThroughCarry(Register8::from(x, 0))
            }
            x if (x & 0b1111_1000) == 0b0010_0000 => {
                Self::ShiftLeftArithmetic(Register8::from(x, 0))
            }
            x if (x & 0b1111_1000) == 0b0010_1000 => {
                Self::ShiftRightArithmetic(Register8::from(x, 0))
            }
            x if (x & 0b1111_1000) == 0b0011_0000 => Self::Swap(Register8::from(x, 0)),
            x if (x & 0b1111_1000) == 0b0011_1000 => Self::ShiftRightLogical(Register8::from(x, 0)),
            x if (x & 0b1100_0000) == 0b0100_0000 => {
                Self::CheckBit(Register8::from(x, 0), U3::from(x, 3))
            }
            x if (x & 0b1100_0000) == 0b1000_0000 => {
                Self::ResetBit(Register8::from(x, 0), U3::from(x, 3))
            }
            x if (x & 0b1100_0000) == 0b1100_0000 => {
                Self::SetBit(Register8::from(x, 0), U3::from(x, 3))
            }
            _ => unreachable!(),
        }
    }
}
