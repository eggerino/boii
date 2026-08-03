use super::*;

#[test]
fn push() {
    let bus = prog([
        0b1100_0101, // push bc
        0b1101_0101, // push de
        0b1110_0101, // push hl
        0b1111_0101, // push af
    ]);
    let mut cpu = cpu(
        bus,
        state(0x0708, 0x0102, 0x0304, 0x0506, 0x0008, 0x0100, false, false),
    );

    step(&mut cpu, 4);

    assert_cpu(
        16,
        state(0x0708, 0x0102, 0x0304, 0x0506, 0x0000, 0x0104, false, false),
        &cpu,
    );
    assert_eq!(cpu.bus[0x0007], 1);
    assert_eq!(cpu.bus[0x0006], 2);
    assert_eq!(cpu.bus[0x0005], 3);
    assert_eq!(cpu.bus[0x0004], 4);
    assert_eq!(cpu.bus[0x0003], 5);
    assert_eq!(cpu.bus[0x0002], 6);
    assert_eq!(cpu.bus[0x0001], 7);
    assert_eq!(cpu.bus[0x0000], 8);
}

#[test]
fn pop() {
    let mut bus = prog([
        0b1100_0001, // pop bc
        0b1101_0001, // pop de
        0b1110_0001, // pop hl
        0b1111_0001, // pop af
    ]);
    bus.write(0, 2).unwrap();
    bus.write(1, 1).unwrap();
    bus.write(2, 4).unwrap();
    bus.write(3, 3).unwrap();
    bus.write(4, 6).unwrap();
    bus.write(5, 5).unwrap();
    bus.write(6, 8).unwrap();
    bus.write(7, 7).unwrap();
    let mut cpu = Cpu::new(bus);

    step(&mut cpu, 4);

    assert_cpu(
        12,
        state(0x0708, 0x0102, 0x0304, 0x0506, 0x0008, 0x0104, false, false),
        &cpu,
    );
}

#[test]
fn add_signed_literal8_to_stack_pointer() {
    let bus = prog([
        0b1110_1000,
        0x7F, // add sp, 0x7F
        0b1110_1000,
        0x7F, // add sp, 0x7F
        0b1110_1000,
        0x7F, // add sp, 0x7F
    ]);
    let mut cpu = cpu(bus, state(0, 0, 0, 0, 0x000F, 0x0100, false, false));

    cpu.step().unwrap();
    assert_cpu(
        4,
        state(0b0010_0000, 0, 0, 0, 0x008E, 0x0102, false, false),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        8,
        state(0b0011_0000, 0, 0, 0, 0x010D, 0x0104, false, false),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        12,
        state(0b0010_0000, 0, 0, 0, 0x018C, 0x0106, false, false),
        &cpu,
    );
}

#[test]
fn load_from_stack_pointer_into_literal16_pointer() {
    let bus = prog([
        0b0000_1000,
        0x01,
        0x00, // ld [0x0001], sp
    ]);
    let mut cpu = cpu(bus, state(0, 0, 0, 0, 0x0807, 0x0100, false, false));

    cpu.step().unwrap();

    assert_cpu(5, state(0, 0, 0, 0, 0x0807, 0x0103, false, false), &cpu);
    assert_eq!(cpu.bus[1], 0x07);
    assert_eq!(cpu.bus[2], 0x08);
}

#[test]
fn load_from_stack_pointer_plus_signed_literal8_into_hl() {
    let data = [
        (0b0010_0000, 0x000F, 0x008E),
        (0b0001_0000, 0x00F0, 0x016F),
        (0b0011_0000, 0x00FF, 0x017E),
    ];
    for (flags, stack_pointer, hl) in data {
        let bus = prog([
            0b1111_1000,
            0x7F, // ld hl, sp + 0x7F
        ]);
        let mut cpu = cpu(bus, state(0, 0, 0, 0, stack_pointer, 0x0100, false, false));

        cpu.step().unwrap();

        assert_cpu(
            3,
            state(flags, 0, 0, hl, stack_pointer, 0x0102, false, false),
            &cpu,
        );
    }
}

#[test]
fn load_from_hl_into_stack_pointer() {
    let bus = prog([0b1111_1001]); // ld sp, hl
    let mut cpu = cpu(bus, state(0, 0, 0, 0xFFFF, 0, 0x0100, false, false));

    cpu.step().unwrap();

    assert_cpu(
        2,
        state(0, 0, 0, 0xFFFF, 0xFFFF, 0x0101, false, false),
        &cpu,
    );
}
