use super::*;

// Bit shift
#[test]
fn prefixed_rotate_left() {
    let mut bus = prog([
        0xCB,
        0b0000_0000, // rlc b
        0xCB,
        0b0000_0001, // rlc c
        0xCB,
        0b0000_0010, // rlc d
        0xCB,
        0b0000_0011, // rlc e
        0xCB,
        0b0000_0100, // rlc h
        0xCB,
        0b0000_0101, // rlc l
        0xCB,
        0b0000_0110, // rlc [hl]
        0xCB,
        0b0000_0111, // rlc a
    ]);
    bus.write(0, 0b1010_1010).unwrap();
    let mut cpu = cpu(
        bus,
        state(0xAA00, 0xAAAA, 0xAAAA, 0, 0, 0x0100, false, false),
    );

    cpu.step().unwrap();
    assert_cpu(
        2,
        state(
            0xAA00 | 0b0001_0000,
            0x55AA,
            0xAAAA,
            0,
            0,
            0x0102,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        4,
        state(
            0xAA00 | 0b0001_0000,
            0x5555,
            0xAAAA,
            0,
            0,
            0x0104,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        6,
        state(
            0xAA00 | 0b0001_0000,
            0x5555,
            0x55AA,
            0,
            0,
            0x0106,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        8,
        state(
            0xAA00 | 0b0001_0000,
            0x5555,
            0x5555,
            0,
            0,
            0x0108,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        10,
        state(
            0xAA00 | 0b1000_0000,
            0x5555,
            0x5555,
            0,
            0,
            0x010A,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        12,
        state(
            0xAA00 | 0b1000_0000,
            0x5555,
            0x5555,
            0,
            0,
            0x010C,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        16,
        state(
            0xAA00 | 0b0001_0000,
            0x5555,
            0x5555,
            0,
            0,
            0x010E,
            false,
            false,
        ),
        &cpu,
    );
    assert_eq!(cpu.bus[0], 0x55);
    cpu.step().unwrap();
    assert_cpu(
        18,
        state(
            0x5500 | 0b0001_0000,
            0x5555,
            0x5555,
            0,
            0,
            0x0110,
            false,
            false,
        ),
        &cpu,
    );
}

#[test]
fn prefixed_rotate_left_through_carry() {
    let mut bus = prog([
        0xCB,
        0b0001_0000, // rl b
        0xCB,
        0b0001_0001, // rl c
        0xCB,
        0b0001_0010, // rl d
        0xCB,
        0b0001_0011, // rl e
        0xCB,
        0b0001_0110, // rl [hl]
        0xCB,
        0b0001_0100, // rl h
        0xCB,
        0b0001_0101, // rl l
        0xCB,
        0b0001_0111, // rl a
    ]);
    bus.write(0, 0b1010_1010).unwrap();
    let mut cpu = cpu(
        bus,
        state(0xAA00, 0xAAAA, 0xAAAA, 0, 0, 0x0100, false, false),
    );

    cpu.step().unwrap();
    assert_cpu(
        2,
        state(
            0xAA00 | 0b0001_0000,
            0x54AA,
            0xAAAA,
            0,
            0,
            0x0102,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        4,
        state(
            0xAA00 | 0b0001_0000,
            0x5455,
            0xAAAA,
            0,
            0,
            0x0104,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        6,
        state(
            0xAA00 | 0b0001_0000,
            0x5455,
            0x55AA,
            0,
            0,
            0x0106,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        8,
        state(
            0xAA00 | 0b0001_0000,
            0x5455,
            0x5555,
            0,
            0,
            0x0108,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        12,
        state(
            0xAA00 | 0b0001_0000,
            0x5455,
            0x5555,
            0,
            0,
            0x010A,
            false,
            false,
        ),
        &cpu,
    );
    assert_eq!(cpu.bus[0], 0x55);
    cpu.step().unwrap();
    assert_cpu(
        14,
        state(
            0xAA00 | 0b0000_0000,
            0x5455,
            0x5555,
            0x0100,
            0,
            0x010C,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        16,
        state(
            0xAA00 | 0b1000_0000,
            0x5455,
            0x5555,
            0x0100,
            0,
            0x010E,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        18,
        state(
            0x5400 | 0b0001_0000,
            0x5455,
            0x5555,
            0x0100,
            0,
            0x0110,
            false,
            false,
        ),
        &cpu,
    );
}

#[test]
fn prefixed_rotate_right() {
    let mut bus = prog([
        0xCB,
        0b0000_1000, // rrc b
        0xCB,
        0b0000_1001, // rrc c
        0xCB,
        0b0000_1010, // rrc d
        0xCB,
        0b0000_1011, // rrc e
        0xCB,
        0b0000_1100, // rrc h
        0xCB,
        0b0000_1101, // rrc l
        0xCB,
        0b0000_1110, // rrc [hl]
        0xCB,
        0b0000_1111, // rrc a
    ]);
    bus.write(0, 0x55).unwrap();
    let mut cpu = cpu(
        bus,
        state(0x5500, 0x5555, 0x5555, 0, 0, 0x0100, false, false),
    );

    cpu.step().unwrap();
    assert_cpu(
        2,
        state(
            0x5500 | 0b0001_0000,
            0xAA55,
            0x5555,
            0,
            0,
            0x0102,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        4,
        state(
            0x5500 | 0b0001_0000,
            0xAAAA,
            0x5555,
            0,
            0,
            0x0104,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        6,
        state(
            0x5500 | 0b0001_0000,
            0xAAAA,
            0xAA55,
            0,
            0,
            0x0106,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        8,
        state(
            0x5500 | 0b0001_0000,
            0xAAAA,
            0xAAAA,
            0,
            0,
            0x0108,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        10,
        state(
            0x5500 | 0b1000_0000,
            0xAAAA,
            0xAAAA,
            0,
            0,
            0x010A,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        12,
        state(
            0x5500 | 0b1000_0000,
            0xAAAA,
            0xAAAA,
            0,
            0,
            0x010C,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        16,
        state(
            0x5500 | 0b0001_0000,
            0xAAAA,
            0xAAAA,
            0,
            0,
            0x010E,
            false,
            false,
        ),
        &cpu,
    );
    assert_eq!(cpu.bus[0], 0xAA);
    cpu.step().unwrap();
    assert_cpu(
        18,
        state(
            0xAA00 | 0b0001_0000,
            0xAAAA,
            0xAAAA,
            0,
            0,
            0x0110,
            false,
            false,
        ),
        &cpu,
    );
}

#[test]
fn prefixed_rotate_right_through_carry() {
    let mut bus = prog([
        0xCB,
        0b0001_1000, // rr b
        0xCB,
        0b0001_1001, // rr c
        0xCB,
        0b0001_1010, // rr d
        0xCB,
        0b0001_1011, // rr e
        0xCB,
        0b0001_1110, // rr [hl]
        0xCB,
        0b0001_1100, // rr h
        0xCB,
        0b0001_1101, // rr l
        0xCB,
        0b0001_1111, // rr a
    ]);
    bus.write(0, 0x55).unwrap();
    let mut cpu = cpu(
        bus,
        state(0x5500, 0x5555, 0x5555, 0, 0, 0x0100, false, false),
    );

    cpu.step().unwrap();
    assert_cpu(
        2,
        state(
            0x5500 | 0b0001_0000,
            0x2A55,
            0x5555,
            0,
            0,
            0x0102,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        4,
        state(
            0x5500 | 0b0001_0000,
            0x2AAA,
            0x5555,
            0,
            0,
            0x0104,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        6,
        state(
            0x5500 | 0b0001_0000,
            0x2AAA,
            0xAA55,
            0,
            0,
            0x0106,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        8,
        state(
            0x5500 | 0b0001_0000,
            0x2AAA,
            0xAAAA,
            0,
            0,
            0x0108,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        12,
        state(
            0x5500 | 0b0001_0000,
            0x2AAA,
            0xAAAA,
            0,
            0,
            0x010A,
            false,
            false,
        ),
        &cpu,
    );
    assert_eq!(cpu.bus[0], 0xAA);
    cpu.step().unwrap();
    assert_cpu(
        14,
        state(
            0x5500 | 0b0000_0000,
            0x2AAA,
            0xAAAA,
            0x8000,
            0,
            0x010C,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        16,
        state(
            0x5500 | 0b1000_0000,
            0x2AAA,
            0xAAAA,
            0x8000,
            0,
            0x010E,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        18,
        state(
            0x2A00 | 0b0001_0000,
            0x2AAA,
            0xAAAA,
            0x8000,
            0,
            0x0110,
            false,
            false,
        ),
        &cpu,
    );
}

#[test]
fn prefixed_shift_left_arithmetic() {
    let mut bus = prog([
        0xCB,
        0b0010_0000, // sla b
        0xCB,
        0b0010_0001, // sla c
        0xCB,
        0b0010_0010, // sla d
        0xCB,
        0b0010_0011, // sla e
        0xCB,
        0b0010_0100, // sla h
        0xCB,
        0b0010_0101, // sla l
        0xCB,
        0b0010_0110, // sla [hl]
        0xCB,
        0b0010_0111, // sla a
    ]);
    bus.write(0, 0xAA).unwrap();
    let mut cpu = cpu(
        bus,
        state(0xAA00, 0xAAAA, 0xAAAA, 0, 0, 0x0100, false, false),
    );

    step(&mut cpu, 4);
    assert_cpu(
        8,
        state(
            0xAA00 | 0b0001_0000,
            0x5454,
            0x5454,
            0,
            0,
            0x0108,
            false,
            false,
        ),
        &cpu,
    );
    step(&mut cpu, 2);
    assert_cpu(
        12,
        state(
            0xAA00 | 0b1000_0000,
            0x5454,
            0x5454,
            0,
            0,
            0x010C,
            false,
            false,
        ),
        &cpu,
    );
    step(&mut cpu, 2);
    assert_cpu(
        18,
        state(
            0x5400 | 0b0001_0000,
            0x5454,
            0x5454,
            0,
            0,
            0x0110,
            false,
            false,
        ),
        &cpu,
    );
    assert_eq!(cpu.bus[0], 0x54);
}

#[test]
fn prefixed_shift_right_arithmetic() {
    let mut bus = prog([
        0xCB,
        0b0010_1000, // sra b
        0xCB,
        0b0010_1001, // sra c
        0xCB,
        0b0010_1010, // sra d
        0xCB,
        0b0010_1011, // sra e
        0xCB,
        0b0010_1100, // sra h
        0xCB,
        0b0010_1101, // sra l
        0xCB,
        0b0010_1110, // sra [hl]
        0xCB,
        0b0010_1111, // sra a
    ]);
    bus.write(0, 0xA5).unwrap();
    let mut cpu = cpu(
        bus,
        state(0xA500, 0xA5A5, 0xA5A5, 0, 0, 0x0100, false, false),
    );

    step(&mut cpu, 4);
    assert_cpu(
        8,
        state(
            0xA500 | 0b0001_0000,
            0xD2D2,
            0xD2D2,
            0,
            0,
            0x0108,
            false,
            false,
        ),
        &cpu,
    );
    step(&mut cpu, 2);
    assert_cpu(
        12,
        state(
            0xA500 | 0b1000_0000,
            0xD2D2,
            0xD2D2,
            0,
            0,
            0x010C,
            false,
            false,
        ),
        &cpu,
    );
    step(&mut cpu, 2);
    assert_cpu(
        18,
        state(
            0xD200 | 0b0001_0000,
            0xD2D2,
            0xD2D2,
            0,
            0,
            0x0110,
            false,
            false,
        ),
        &cpu,
    );
    assert_eq!(cpu.bus[0], 0xD2);
}

#[test]
fn prefixed_swap() {
    let mut bus = prog([
        0xCB,
        0b0011_0000, // swap b
        0xCB,
        0b0011_0001, // swap c
        0xCB,
        0b0011_0010, // swap d
        0xCB,
        0b0011_0011, // swap e
        0xCB,
        0b0011_0100, // swap h
        0xCB,
        0b0011_0101, // swap l
        0xCB,
        0b0011_0110, // swap [hl]
        0xCB,
        0b0011_0111, // swap a
    ]);
    bus.write(0, 0x1E).unwrap();
    let mut cpu = cpu(
        bus,
        state(0x1E00, 0x1E1E, 0x1E1E, 0, 0, 0x0100, false, false),
    );

    step(&mut cpu, 5);
    assert_cpu(
        10,
        state(
            0x1E00 | 0b1000_0000,
            0xE1E1,
            0xE1E1,
            0,
            0,
            0x010A,
            false,
            false,
        ),
        &cpu,
    );
    step(&mut cpu, 3);
    assert_cpu(
        18,
        state(
            0xE100 | 0b0000_0000,
            0xE1E1,
            0xE1E1,
            0,
            0,
            0x0110,
            false,
            false,
        ),
        &cpu,
    );
    assert_eq!(cpu.bus[0], 0xE1);
}

#[test]
fn prefixed_shift_right_logical() {
    let mut bus = prog([
        0xCB,
        0b0011_1000, // srl b
        0xCB,
        0b0011_1001, // srl c
        0xCB,
        0b0011_1010, // srl d
        0xCB,
        0b0011_1011, // srl e
        0xCB,
        0b0011_1100, // srl h
        0xCB,
        0b0011_1101, // srl l
        0xCB,
        0b0011_1110, // srl [hl]
        0xCB,
        0b0011_1111, // srl a
    ]);
    bus.write(0, 0xA5).unwrap();
    let mut cpu = cpu(
        bus,
        state(0xA500, 0xA5A5, 0xA5A5, 0, 0, 0x0100, false, false),
    );

    step(&mut cpu, 4);
    assert_cpu(
        8,
        state(
            0xA500 | 0b0001_0000,
            0x5252,
            0x5252,
            0,
            0,
            0x0108,
            false,
            false,
        ),
        &cpu,
    );
    step(&mut cpu, 2);
    assert_cpu(
        12,
        state(
            0xA500 | 0b1000_0000,
            0x5252,
            0x5252,
            0,
            0,
            0x010C,
            false,
            false,
        ),
        &cpu,
    );
    step(&mut cpu, 2);
    assert_cpu(
        18,
        state(
            0x5200 | 0b0001_0000,
            0x5252,
            0x5252,
            0,
            0,
            0x0110,
            false,
            false,
        ),
        &cpu,
    );
    assert_eq!(cpu.bus[0], 0x52);
}

// 16 Bit instructions
// Bit flag
#[test]
fn prefixed_check_bit() {
    let mut bus = prog([
        0xCB,
        0b0100_0000, // bit 0, b
        0xCB,
        0b0100_1001, // bit 1, c
        0xCB,
        0b0101_0010, // bit 2, d
        0xCB,
        0b0101_1011, // bit 3, e
        0xCB,
        0b0110_0100, // bit 4, h
        0xCB,
        0b0110_1101, // bit 5, l
        0xCB,
        0b0111_0110, // bit 6, [hl]
        0xCB,
        0b0111_1111, // bit 7, a
    ]);
    bus.write(0, 0x40).unwrap();
    let mut cpu = cpu(
        bus,
        state(0x8000, 0x0102, 0x0408, 0, 0, 0x0100, false, false),
    );

    cpu.step().unwrap();
    assert_cpu(
        2,
        state(
            0x8000 | 0b1010_0000,
            0x0102,
            0x0408,
            0,
            0,
            0x0102,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        4,
        state(
            0x8000 | 0b1010_0000,
            0x0102,
            0x0408,
            0,
            0,
            0x0104,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        6,
        state(
            0x8000 | 0b1010_0000,
            0x0102,
            0x0408,
            0,
            0,
            0x0106,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        8,
        state(
            0x8000 | 0b1010_0000,
            0x0102,
            0x0408,
            0,
            0,
            0x0108,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        10,
        state(
            0x8000 | 0b0010_0000,
            0x0102,
            0x0408,
            0,
            0,
            0x010A,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        12,
        state(
            0x8000 | 0b0010_0000,
            0x0102,
            0x0408,
            0,
            0,
            0x010C,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        15,
        state(
            0x8000 | 0b1010_0000,
            0x0102,
            0x0408,
            0,
            0,
            0x010E,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        17,
        state(
            0x8000 | 0b1010_0000,
            0x0102,
            0x0408,
            0,
            0,
            0x0110,
            false,
            false,
        ),
        &cpu,
    );
}

#[test]
fn prefixed_set_bit() {
    let registers: [u8; 8] = [6, 0, 1, 2, 3, 4, 5, 7];
    let program: Vec<_> = registers
        .into_iter()
        .flat_map(|r| (0..8).map(move |b| (r, b)))
        .map(|(r, b)| 0b1100_0000 | r | (b << 3))
        .flat_map(|x| [0xCB, x])
        .collect();
    let bus = prog_vec(program);
    let mut cpu = Cpu::new(bus);

    step(&mut cpu, 64);

    assert_cpu(
        8 * 4 + 56 * 2,
        state(
            0xFF00,
            0xFFFF,
            0xFFFF,
            0xFFFF,
            0,
            0x0100 + 2 * 64,
            false,
            false,
        ),
        &cpu,
    );
    assert_eq!(cpu.bus[0], 0xFF);
}

#[test]
fn prefixed_reset_bit() {
    let registers: [u8; 8] = [0, 1, 2, 3, 4, 5, 6, 7];
    let program: Vec<_> = registers
        .into_iter()
        .flat_map(|r| (0..8).map(move |b| (r, b)))
        .map(|(r, b)| 0b1000_0000 | r | (b << 3))
        .flat_map(|x| [0xCB, x])
        .collect();
    let mut bus = prog_vec(program);
    bus.write(0, 0xFF).unwrap();
    let mut cpu = cpu(
        bus,
        state(0xFF00, 0xFFFF, 0xFFFF, 0xFFFF, 0, 0x0100, false, false),
    );

    step(&mut cpu, 64);

    assert_cpu(
        8 * 4 + 56 * 2,
        state(
            0x0000,
            0x0000,
            0x0000,
            0x0000,
            0,
            0x0100 + 2 * 64,
            false,
            false,
        ),
        &cpu,
    );
    assert_eq!(cpu.bus[0], 0x00);
}
