use super::*;

#[test]
fn increment_register16() {
    let bus = prog([
        0b0000_0011, // inc bc
        0b0001_0011, // inc de
        0b0010_0011, // inc hl
        0b0011_0011, // inc sp
    ]);
    let mut cpu = Cpu::new(bus);

    step(&mut cpu, 4);

    assert_cpu(8, state(0, 1, 1, 1, 1, 0x0104, false, false), &cpu);
}

#[test]
fn decrement_register16() {
    let bus = prog([
        0b0000_1011, // dec bc
        0b0001_1011, // dec de
        0b0010_1011, // dec hl
        0b0011_1011, // dec sp
    ]);
    let mut cpu = Cpu::new(bus);

    step(&mut cpu, 4);

    assert_cpu(
        8,
        state(0, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0x0104, false, false),
        &cpu,
    );
}

#[test]
fn add_register16_to_hl() {
    let bus = prog([
        0b0000_1001, // add hl, bc
        0b0001_1001, // add hl, de
        0b0010_1001, // add hl, hl
        0b0011_1001, // add hl, sp
    ]);
    let mut cpu = cpu(
        bus,
        state(0, 0x0FFF, 0x0001, 0, 0xE001, 0x0100, false, false),
    );

    cpu.step().unwrap();
    assert_cpu(
        2,
        state(
            0b0000_0000,
            0x0FFF,
            0x0001,
            0x0FFF,
            0xE001,
            0x0101,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        4,
        state(
            0b0010_0000,
            0x0FFF,
            0x0001,
            0x1000,
            0xE001,
            0x0102,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        6,
        state(
            0b0000_0000,
            0x0FFF,
            0x0001,
            0x2000,
            0xE001,
            0x0103,
            false,
            false,
        ),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        8,
        state(
            0b0001_0000,
            0x0FFF,
            0x0001,
            0x0001,
            0xE001,
            0x0104,
            false,
            false,
        ),
        &cpu,
    );
}
