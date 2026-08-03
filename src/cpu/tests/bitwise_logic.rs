use super::*;

#[test]
fn complement_a() {
    let bus = prog([
        0b0010_1111, // cpl
    ]);
    let mut cpu = cpu(
        bus,
        state(0b1010_1010_0000_0000, 0, 0, 0, 0, 0x0100, false, false),
    );

    cpu.step().unwrap();

    assert_cpu(
        1,
        state(0b0101_0101_0110_0000, 0, 0, 0, 0, 0x0101, false, false),
        &cpu,
    );
}

#[test]
fn and_with_a() {
    let bus = prog([
        0b1010_0000, // and a, b
        0b1010_0001, // and a, c
        0b1010_0010, // and a, d
        0b1010_0011, // and a, e
        0b1010_0100, // and a, h
        0b1010_0101, // and a, l
        0b1010_0110, // and a, [hl]
        0b1010_0111, // and a, a
        0x00,        // [hl]
    ]);
    let mut cpu = cpu(
        bus,
        state(0xFF00, 0x0001, 0x02F3, 0x0108, 0, 0x0100, false, false),
    );

    step(&mut cpu, 8);
    assert_cpu(
        9,
        state(
            0x0000 | 0b1010_0000,
            0x0001,
            0x02F3,
            0x0108,
            0,
            0x0108,
            false,
            false,
        ),
        &cpu,
    );
}

#[test]
fn and_literal8_with_a() {
    let bus = prog([
        0b1110_0110,
        0xF0, // and a, 0xF0
    ]);
    let mut cpu = cpu(bus, state(0x0F00, 0, 0, 0, 0, 0x0100, false, false));

    cpu.step().unwrap();
    assert_cpu(
        2,
        state(0x0000 | 0b1010_0000, 0, 0, 0, 0, 0x0102, false, false),
        &cpu,
    );
}

#[test]
fn xor_with_a() {
    let bus = prog([
        0b1010_1000, // xor a, b
        0b1010_1001, // xor a, c
        0b1010_1010, // xor a, d
        0b1010_1011, // xor a, e
        0b1010_1100, // xor a, h
        0b1010_1101, // xor a, l
        0b1010_1110, // xor a, [hl]
        0b1010_1111, // xor a, a
        0x00,        // [hl]
    ]);
    let mut cpu = cpu(
        bus,
        state(0xFF00, 0x0001, 0x02F3, 0x0108, 0, 0x0100, false, false),
    );

    step(&mut cpu, 8);

    assert_cpu(
        9,
        state(
            0x0000 | 0b1000_0000,
            0x0001,
            0x02F3,
            0x0108,
            0,
            0x0108,
            false,
            false,
        ),
        &cpu,
    );
}

#[test]
fn xor_literal8_with_a() {
    let bus = prog([
        0b1110_1110,
        0xF0, // xor a, 0xF0
    ]);
    let mut cpu = cpu(bus, state(0xF000, 0, 0, 0, 0, 0x0100, false, false));

    cpu.step().unwrap();
    assert_cpu(
        2,
        state(0x0000 | 0b1000_0000, 0, 0, 0, 0, 0x0102, false, false),
        &cpu,
    );
}

#[test]
fn or_with_a() {
    let bus = prog([
        0b1011_0000, // or a, b
        0b1011_0001, // or a, c
        0b1011_0010, // or a, d
        0b1011_0011, // or a, e
        0b1011_0100, // or a, h
        0b1011_0101, // or a, l
        0b1011_0110, // or a, [hl]
        0b1011_0111, // or a, a
        0x04,        // [hl]
    ]);
    let mut cpu = cpu(
        bus,
        state(0x0000, 0x0001, 0x02F3, 0x0108, 0, 0x0100, false, false),
    );

    cpu.step().unwrap();
    assert_cpu(
        1,
        state(
            0x0000 | 0b1000_0000,
            0x0001,
            0x02F3,
            0x0108,
            0,
            0x0101,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        2,
        state(
            0x0100 | 0b0000_0000,
            0x0001,
            0x02F3,
            0x0108,
            0,
            0x0102,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        3,
        state(
            0x0300 | 0b0000_0000,
            0x0001,
            0x02F3,
            0x0108,
            0,
            0x0103,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        4,
        state(
            0xF300 | 0b0000_0000,
            0x0001,
            0x02F3,
            0x0108,
            0,
            0x0104,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        5,
        state(
            0xF300 | 0b0000_0000,
            0x0001,
            0x02F3,
            0x0108,
            0,
            0x0105,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        6,
        state(
            0xFB00 | 0b0000_0000,
            0x0001,
            0x02F3,
            0x0108,
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
            0xFF00 | 0b0000_0000,
            0x0001,
            0x02F3,
            0x0108,
            0,
            0x0107,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        9,
        state(
            0xFF00 | 0b0000_0000,
            0x0001,
            0x02F3,
            0x0108,
            0,
            0x0108,
            false,
            false,
        ),
        &cpu,
    );
}

#[test]
fn or_literal8_with_a() {
    let bus = prog([
        0b1111_0110,
        0x00, // or a, 0x00
    ]);
    let mut cpu = cpu(bus, state(0x0000, 0, 0, 0, 0, 0x0100, false, false));

    cpu.step().unwrap();
    assert_cpu(
        2,
        state(0x0000 | 0b1000_0000, 0, 0, 0, 0, 0x0102, false, false),
        &cpu,
    );
}
