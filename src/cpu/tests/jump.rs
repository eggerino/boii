use super::*;

#[test]
fn jump_relative() {
    let bus = prog([
        0b0001_1000,
        0x01, // jr 1
        0x00, // nop
        0b0001_1000,
        0xFF, // jr -1
    ]);
    let mut cpu = Cpu::new(bus);

    step(&mut cpu, 2);

    assert_cpu(6, state(0, 0, 0, 0, 0, 0x0104, false, false), &cpu);
}

#[test]
fn conditional_jump_relative() {
    let data = [
        (0b1000_0000, 0b0010_0000, 1, 2, 0x0102),    // jr nz, 1
        (0b0000_0000, 0b0010_0000, 1, 3, 0x0103),    // jr nz, 1
        (0b0000_0000, 0b0010_0000, 0xFF, 3, 0x0101), // jr nz, -1
        (0b0000_0000, 0b0010_1000, 1, 2, 0x0102),    // jr z, 1
        (0b1000_0000, 0b0010_1000, 1, 3, 0x0103),    // jr z, 1
        (0b0001_0000, 0b0011_0000, 1, 2, 0x0102),    // jr nc, 1
        (0b0000_0000, 0b0011_0000, 1, 3, 0x0103),    // jr nc, 1
        (0b0000_0000, 0b0011_1000, 1, 2, 0x0102),    // jr c, 1
        (0b0001_0000, 0b0011_1000, 1, 3, 0x0103),    // jr c, 1
    ];

    for (flags, opcode, address, expected_ticks, expected_prog_counter) in data {
        let bus = prog([opcode, address]);
        let mut cpu = cpu(bus, state(flags, 0, 0, 0, 0, 0x0100, false, false));

        cpu.step().unwrap();

        assert_cpu(
            expected_ticks,
            state(flags, 0, 0, 0, 0, expected_prog_counter, false, false),
            &cpu,
        );
    }
}

#[test]
fn jump() {
    let bus = prog([
        0b1100_0011,
        0x01,
        0x02, // jp 0
    ]);
    let mut cpu = Cpu::new(bus);

    cpu.step().unwrap();

    assert_cpu(4, state(0, 0, 0, 0, 0, 0x0201, false, false), &cpu);
}

#[test]
fn conditional_jump() {
    let bus = prog([
        0b1100_0010,
        0x04,
        0x01, // jp nz, 0x0104
        0,    // nop
        0b1100_1010,
        0x00,
        0x00, // jp z, 0
        0,    // nop
        0b1101_0010,
        0x0C,
        0x01, // jp nc, 0x010C
        0,    // nop
        0b1101_1010,
        0x00,
        0x00, // jp c, 0
    ]);
    let mut cpu = Cpu::new(bus);

    step(&mut cpu, 5);

    assert_cpu(15, state(0, 0, 0, 0, 0, 0x010F, false, false), &cpu);
}

#[test]
fn jump_hl() {
    let bus = prog([
        0b1110_1001, // jp hl
    ]);
    let mut cpu = Cpu::new(bus);

    cpu.step().unwrap();

    assert_cpu(1, state(0, 0, 0, 0, 0, 0x0, false, false), &cpu);
}

#[test]
fn call() {
    let bus = prog([
        0b1100_1101,
        0x02,
        0x01, // call 0x0102
    ]);
    let mut cpu = cpu(bus, state(0, 0, 0, 0, 0x0005, 0x0100, false, false));

    cpu.step().unwrap();

    assert_cpu(6, state(0, 0, 0, 0, 0x0003, 0x0102, false, false), &cpu);
    assert_eq!(cpu.bus[0x04], 0x01);
    assert_eq!(cpu.bus[0x03], 0x03);
}

#[test]
fn conditional_call() {
    let bus = prog([
        0b1100_0100,
        0x04,
        0x01, // call nz, 0x0104
        0,    // nop
        0b1100_1100,
        0x08,
        0x01, // call z, 0x0108
        0,    // nop
        0b1101_0100,
        0x0C,
        0x01, // call nc, 0x010C
        0,    // nop
        0b1101_1100,
        0x00,
        0x00, // call c, 0
    ]);
    let mut cpu = cpu(bus, state(0, 0, 0, 0, 0x0004, 0x0100, false, false));

    step(&mut cpu, 5);

    assert_cpu(19, state(0, 0, 0, 0, 0x0000, 0x010F, false, false), &cpu);
    assert_eq!(cpu.bus[0x03], 0x01);
    assert_eq!(cpu.bus[0x02], 0x03);
    assert_eq!(cpu.bus[0x01], 0x01);
    assert_eq!(cpu.bus[0x00], 0x0B);
}

#[test]
fn restart() {
    let bus = prog([
        0b1111_1111, // rst 7
    ]);
    let mut cpu = cpu(bus, state(0, 0, 0, 0, 0x0005, 0x0100, false, false));

    cpu.step().unwrap();

    assert_cpu(4, state(0, 0, 0, 0, 0x0003, 7 * 8, false, false), &cpu);
    assert_eq!(cpu.bus[0x04], 0x01);
    assert_eq!(cpu.bus[0x03], 0x01);
}

#[test]
fn return_() {
    let mut bus = prog([
        0b1100_1001, // ret
    ]);
    bus.write(0, 0x02).unwrap();
    bus.write(1, 0x01).unwrap();
    let mut cpu = Cpu::new(bus);

    cpu.step().unwrap();

    assert_cpu(4, state(0, 0, 0, 0, 2, 0x0102, false, false), &cpu);
}

#[test]
fn conditional_return() {
    let mut bus = prog([
        0b1100_0000, // ret nz
        0,           // nop
        0b1100_1000, // ret z
        0,           // nop
        0b1101_0000, // ret nc
        0,           // nop
        0b1101_1000, // ret c
    ]);
    bus.write(0, 0x02).unwrap();
    bus.write(1, 0x01).unwrap();
    bus.write(2, 0x06).unwrap();
    bus.write(3, 0x01).unwrap();
    let mut cpu = Cpu::new(bus);

    step(&mut cpu, 5);

    assert_cpu(15, state(0, 0, 0, 0, 0x0004, 0x0107, false, false), &cpu);
}

#[test]
fn return_interrupt() {
    let mut bus = prog([
        0b1101_1001, // reti
    ]);
    bus.write(0, 0x02).unwrap();
    bus.write(1, 0x01).unwrap();
    let mut cpu = Cpu::new(bus);

    cpu.step().unwrap();

    assert_cpu(4, state(0, 0, 0, 0, 2, 0x0102, false, true), &cpu);
}
