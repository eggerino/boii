use super::*;

#[test]
fn increment_register8() {
    let bus = prog([
        0b0000_0100, // inc b
        0b0000_1100, // inc c
        0b0001_0100, // inc d
        0b0001_1100, // inc e
        0b0011_0100, // inc [hl]
        0b0010_0100, // inc h
        0b0010_1100, // inc l
        0b0011_1100, // inc a
    ]);
    let mut cpu = cpu(bus, state(0, 0xFF0F, 0, 0, 0, 0x0100, false, false));

    cpu.step().unwrap();
    assert_cpu(
        1,
        state(0x0000 | 0b1010_0000, 0x000F, 0, 0, 0, 0x0101, false, false),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        2,
        state(0x0000 | 0b0010_0000, 0x0010, 0, 0, 0, 0x0102, false, false),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        3,
        state(
            0x0000 | 0b0000_0000,
            0x0010,
            0x0100,
            0,
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
            0x0000 | 0b0000_0000,
            0x0010,
            0x0101,
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
        7,
        state(
            0x0000 | 0b0000_0000,
            0x0010,
            0x0101,
            0,
            0,
            0x0105,
            false,
            false,
        ),
        &cpu,
    );
    assert_eq!(cpu.bus[0x0000], 0x01);
    cpu.step().unwrap();
    assert_cpu(
        8,
        state(
            0x0000 | 0b0000_0000,
            0x0010,
            0x0101,
            0x0100,
            0,
            0x0106,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        9,
        state(
            0x0000 | 0b0000_0000,
            0x0010,
            0x0101,
            0x0101,
            0,
            0x0107,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        10,
        state(
            0x0100 | 0b0000_0000,
            0x0010,
            0x0101,
            0x0101,
            0,
            0x0108,
            false,
            false,
        ),
        &cpu,
    );
}

#[test]
fn decrement_register8() {
    let bus = prog([
        0b0000_0101, // dec b
        0b0000_1101, // dec c
        0b0001_0101, // dec d
        0b0001_1101, // dec e
        0b0011_0101, // dec [hl]
        0b0010_0101, // dec h
        0b0010_1101, // dec l
        0b0011_1101, // dec a
    ]);
    let mut cpu = cpu(bus, state(0, 0x0102, 0, 0, 0, 0x0100, false, false));

    cpu.step().unwrap();
    assert_cpu(
        1,
        state(0x0000 | 0b1100_0000, 0x0002, 0, 0, 0, 0x0101, false, false),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        2,
        state(0x0000 | 0b0100_0000, 0x0001, 0, 0, 0, 0x0102, false, false),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        3,
        state(
            0x0000 | 0b0110_0000,
            0x0001,
            0xFF00,
            0,
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
            0x0000 | 0b0110_0000,
            0x0001,
            0xFFFF,
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
        7,
        state(
            0x0000 | 0b0110_0000,
            0x0001,
            0xFFFF,
            0,
            0,
            0x0105,
            false,
            false,
        ),
        &cpu,
    );
    assert_eq!(cpu.bus[0x0000], 0xFF);
    cpu.step().unwrap();
    assert_cpu(
        8,
        state(
            0x0000 | 0b0110_0000,
            0x0001,
            0xFFFF,
            0xFF00,
            0,
            0x0106,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        9,
        state(
            0x0000 | 0b0110_0000,
            0x0001,
            0xFFFF,
            0xFFFF,
            0,
            0x0107,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        10,
        state(
            0xFF00 | 0b0110_0000,
            0x0001,
            0xFFFF,
            0xFFFF,
            0,
            0x0108,
            false,
            false,
        ),
        &cpu,
    );
}

#[test]
fn add_to_a() {
    let mut bus = prog([
        0b1000_0000, // add a, b
        0b1000_0001, // add a, c
        0b1000_0010, // add a, d
        0b1000_0011, // add a, e
        0b1000_0100, // add a, h
        0b1000_0101, // add a, l
        0b1000_0110, // add a, [hl]
        0b1000_0111, // add a, a
    ]);
    bus.write(0, 0x10).unwrap();
    let mut cpu = cpu(
        bus,
        state(0, 0x0001, 0x0FF3, 0x0000, 0, 0x0100, false, false),
    );

    cpu.step().unwrap();
    assert_cpu(
        1,
        state(
            0x0000 | 0b1000_0000,
            0x0001,
            0x0FF3,
            0x0000,
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
            0x0FF3,
            0x0000,
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
            0x1000 | 0b0010_0000,
            0x0001,
            0x0FF3,
            0x0000,
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
            0x0300 | 0b0001_0000,
            0x0001,
            0x0FF3,
            0x0000,
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
            0x0300 | 0b0000_0000,
            0x0001,
            0x0FF3,
            0x0000,
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
            0x0300 | 0b0000_0000,
            0x0001,
            0x0FF3,
            0x0000,
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
            0x1300 | 0b0000_0000,
            0x0001,
            0x0FF3,
            0x0000,
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
            0x2600 | 0b0000_0000,
            0x0001,
            0x0FF3,
            0x0000,
            0,
            0x0108,
            false,
            false,
        ),
        &cpu,
    );
}

#[test]
fn add_literal8_to_a() {
    let bus = prog([
        0b1100_0110,
        0x10, // add a, 0x10
    ]);
    let mut cpu = Cpu::new(bus);

    cpu.step().unwrap();
    assert_cpu(
        2,
        state(0x1000 | 0b0000_0000, 0, 0, 0, 0, 0x0102, false, false),
        &cpu,
    );
}

#[test]
fn add_to_a_carry() {
    let bus = prog([
        0b1000_1000, // adc a, b
        0b1000_1001, // adc a, c
    ]);
    let mut cpu = cpu(bus, state(0xFF00, 0x0101, 0, 0, 0, 0x0100, false, false));

    step(&mut cpu, 2);
    assert_cpu(
        2,
        state(0x0200 | 0b0000_0000, 0x0101, 0, 0, 0, 0x0102, false, false),
        &cpu,
    );
}

#[test]
fn add_literal8_to_a_carry() {
    let bus = prog([
        0b1100_1110,
        0x01, // adc a, 0x01
        0b1100_1110,
        0x01, // adc a, 0x01
    ]);
    let mut cpu = cpu(bus, state(0xFF00, 0, 0, 0, 0, 0x0100, false, false));

    step(&mut cpu, 2);
    assert_cpu(
        4,
        state(0x0200 | 0b0000_0000, 0, 0, 0, 0, 0x0104, false, false),
        &cpu,
    );
}

#[test]
fn subtract_from_a() {
    let mut bus = prog([
        0b1001_0000, // sub a, b
        0b1001_0001, // sub a, c
        0b1001_0010, // sub a, d
        0b1001_0011, // sub a, e
        0b1001_0100, // sub a, h
        0b1001_0101, // sub a, l
        0b1001_0110, // sub a, [hl]
        0b1001_0111, // sub a, a
    ]);
    bus.write(0, 0x05).unwrap();
    let mut cpu = cpu(
        bus,
        state(0, 0x0001, 0x020F, 0x0000, 0, 0x0100, false, false),
    );

    cpu.step().unwrap();
    assert_cpu(
        1,
        state(
            0x0000 | 0b1100_0000,
            0x0001,
            0x020F,
            0x0000,
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
            0xFF00 | 0b0111_0000,
            0x0001,
            0x020F,
            0x0000,
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
            0xFD00 | 0b0100_0000,
            0x0001,
            0x020F,
            0x0000,
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
            0xEE00 | 0b0110_0000,
            0x0001,
            0x020F,
            0x0000,
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
            0xEE00 | 0b0100_0000,
            0x0001,
            0x020F,
            0x0000,
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
            0xEE00 | 0b0100_0000,
            0x0001,
            0x020F,
            0x0000,
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
            0xE900 | 0b0100_0000,
            0x0001,
            0x020F,
            0x0000,
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
            0x0000 | 0b1100_0000,
            0x0001,
            0x020F,
            0x0000,
            0,
            0x0108,
            false,
            false,
        ),
        &cpu,
    );
}

#[test]
fn subtract_literal8_from_a() {
    let bus = prog([
        0b1101_0110,
        0x0F, // sub a, 0x0F
    ]);
    let mut cpu = Cpu::new(bus);

    cpu.step().unwrap();
    assert_cpu(
        2,
        state(0xF100 | 0b0111_0000, 0, 0, 0, 0, 0x0102, false, false),
        &cpu,
    );
}

#[test]
fn subtract_from_a_carry() {
    let bus = prog([
        0b1001_1000, // sbc a, b
        0b1001_1001, // sbc a, c
    ]);
    let mut cpu = cpu(bus, state(0, 0x0101, 0, 0, 0, 0x0100, false, false));

    step(&mut cpu, 2);
    assert_cpu(
        2,
        state(0xFD00 | 0b0100_0000, 0x0101, 0, 0, 0, 0x0102, false, false),
        &cpu,
    );
}

#[test]
fn subtract_literal8_from_a_carry() {
    let bus = prog([
        0b1101_1110,
        0x01, // sbc a, 0x01
        0b1101_1110,
        0x01, // sbc a, 0x01
    ]);
    let mut cpu = Cpu::new(bus);

    step(&mut cpu, 2);
    assert_cpu(
        4,
        state(0xFD00 | 0b0100_0000, 0, 0, 0, 0, 0x0104, false, false),
        &cpu,
    );
}

#[test]
fn compare_to_a() {
    let mut bus = prog([
        0b1011_1000, // cp a, b
        0b1011_1001, // cp a, c
        0b1011_1010, // cp a, d
        0b1011_1011, // cp a, e
        0b1011_1100, // cp a, h
        0b1011_1101, // cp a, l
        0b1011_1110, // cp a, [hl]
        0b1011_1111, // cp a, a
    ]);
    bus.write(0, 0x05).unwrap();
    let mut cpu = cpu(
        bus,
        state(0x1000, 0x1001, 0x2000, 0x0000, 0, 0x0100, false, false),
    );

    cpu.step().unwrap();
    assert_cpu(
        1,
        state(
            0x1000 | 0b1100_0000,
            0x1001,
            0x2000,
            0x0000,
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
            0x1000 | 0b0110_0000,
            0x1001,
            0x2000,
            0x0000,
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
            0x1000 | 0b0101_0000,
            0x1001,
            0x2000,
            0x0000,
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
            0x1000 | 0b0100_0000,
            0x1001,
            0x2000,
            0x0000,
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
            0x1000 | 0b0100_0000,
            0x1001,
            0x2000,
            0x0000,
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
            0x1000 | 0b0100_0000,
            0x1001,
            0x2000,
            0x0000,
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
            0x1000 | 0b0110_0000,
            0x1001,
            0x2000,
            0x0000,
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
            0x1000 | 0b1100_0000,
            0x1001,
            0x2000,
            0x0000,
            0,
            0x0108,
            false,
            false,
        ),
        &cpu,
    );
}

#[test]
fn compare_literal8_to_a() {
    let bus = prog([
        0b1111_1110,
        0x0F, // cp a, 0x0F
    ]);
    let mut cpu = Cpu::new(bus);

    cpu.step().unwrap();
    assert_cpu(
        2,
        state(0x0000 | 0b0111_0000, 0, 0, 0, 0, 0x0102, false, false),
        &cpu,
    );
}
