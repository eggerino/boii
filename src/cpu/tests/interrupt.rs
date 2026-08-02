use super::*;

#[test]
fn halt() {
    let mut bus = prog([0b111_0110]); // halt
    ensure_size(&mut bus, 0x1_0000);
    let mut cpu = Cpu::new(bus);

    step(&mut cpu, 2);

    assert_cpu(2, state(0, 0, 0, 0, 0, 0x0101, true, false), &cpu);
}

#[test]
fn enable_interrupt() {
    let bus = prog([
        0b1111_1011, // ei
        0,           // nop
    ]);
    let mut cpu = Cpu::new(bus);

    cpu.step().unwrap();
    assert_cpu(1, state(0, 0, 0, 0, 0, 0x0101, false, false), &cpu);

    cpu.step().unwrap();
    assert_cpu(2, state(0, 0, 0, 0, 0, 0x0102, false, true), &cpu);
}

#[test]
fn disable_interrupt() {
    let mut bus = prog([
        0b1111_0011, // di
    ]);
    ensure_size(&mut bus, 0x1_0000);
    let mut cpu = Cpu::new(bus);

    cpu.step().unwrap();
    assert_cpu(1, state(0, 0, 0, 0, 0, 0x0101, false, false), &cpu);
}

#[test]
fn enable_and_disable_interrupt() {
    let mut bus = prog([
        0b1111_1011, // ei
        0b1111_0011, // di
        0,           // nop
        0,           // nop
        0b1111_1011, // ei
        0,           // nop
        0,           // nop
        0b1111_0011, // di
        0,           // nop
        0,           // nop
    ]);
    ensure_size(&mut bus, 0x1_0000);
    let mut cpu = Cpu::new(bus);

    cpu.step().unwrap();
    assert_cpu(1, state(0, 0, 0, 0, 0, 0x0101, false, false), &cpu);
    cpu.step().unwrap();
    assert_cpu(2, state(0, 0, 0, 0, 0, 0x0102, false, false), &cpu);
    cpu.step().unwrap();
    assert_cpu(3, state(0, 0, 0, 0, 0, 0x0103, false, false), &cpu);
    cpu.step().unwrap();
    assert_cpu(4, state(0, 0, 0, 0, 0, 0x0104, false, false), &cpu);
    cpu.step().unwrap();
    assert_cpu(5, state(0, 0, 0, 0, 0, 0x0105, false, false), &cpu);
    cpu.step().unwrap();
    assert_cpu(6, state(0, 0, 0, 0, 0, 0x0106, false, true), &cpu);
    cpu.step().unwrap();
    assert_cpu(7, state(0, 0, 0, 0, 0, 0x0107, false, true), &cpu);
    cpu.step().unwrap();
    assert_cpu(8, state(0, 0, 0, 0, 0, 0x0108, false, false), &cpu);
    cpu.step().unwrap();
    assert_cpu(9, state(0, 0, 0, 0, 0, 0x0109, false, false), &cpu);
    cpu.step().unwrap();
    assert_cpu(10, state(0, 0, 0, 0, 0, 0x010A, false, false), &cpu);
}
