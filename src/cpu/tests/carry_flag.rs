use super::*;

#[test]
fn set_carry_flag() {
    let bus = prog([
        0b0011_0111, // scf
    ]);
    let mut cpu = Cpu::new(bus);

    cpu.step().unwrap();

    assert_cpu(
        1,
        state(0x0000 | 0b0001_0000, 0, 0, 0, 0, 0x0101, false, false),
        &cpu,
    );
}

#[test]
fn complement_carry_flag() {
    let bus = prog([
        0b0011_1111, // ccf
    ]);
    let mut cpu = cpu(
        bus,
        state(0x0000 | 0b0001_0000, 0, 0, 0, 0, 0x0100, false, false),
    );

    cpu.step().unwrap();

    assert_cpu(
        1,
        state(0x0000 | 0b0000_0000, 0, 0, 0, 0, 0x0101, false, false),
        &cpu,
    );
}
