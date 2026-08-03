use super::*;

#[test]
fn rotate_left_a_with_and_without_carry() {
    let bus = prog([
        0b0000_0111, // rlca
        0b0001_0111, // rla
    ]);
    let mut cpu = cpu(
        bus,
        state(0b1000_1110_0000_0000, 0, 0, 0, 0, 0x0100, false, false),
    );

    cpu.step().unwrap();
    assert_cpu(
        1,
        state(0b0001_1101_0001_0000, 0, 0, 0, 0, 0x0101, false, false),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        2,
        state(0b0011_1011_0000_0000, 0, 0, 0, 0, 0x0102, false, false),
        &cpu,
    );
}

#[test]
fn rotate_right_a_with_and_without_carry() {
    let bus = prog([
        0b0000_1111, // rrca
        0b0001_1111, // rra
    ]);
    let mut cpu = cpu(
        bus,
        state(0b0111_0001_0000_0000, 0, 0, 0, 0, 0x0100, false, false),
    );

    cpu.step().unwrap();
    assert_cpu(
        1,
        state(0b1011_1000_0001_0000, 0, 0, 0, 0, 0x0101, false, false),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        2,
        state(0b1101_1100_0000_0000, 0, 0, 0, 0, 0x0102, false, false),
        &cpu,
    );
}
