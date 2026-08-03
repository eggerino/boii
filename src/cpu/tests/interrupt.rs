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

#[test]
fn interrupt_calls() {
    let data = [
        (0b0000_0001, 0x0040),
        (0b0000_0010, 0x0048),
        (0b0000_0100, 0x0050),
        (0b0000_1000, 0x0058),
        (0b0001_0000, 0x0060),
    ];

    for (interrupt_mask, target_address) in data {
        let mut bus = prog([]);
        ensure_size(&mut bus, 0x1_0000);

        bus.write(0xFF0F, interrupt_mask).unwrap(); // Prepare the interrupt
        bus.write(0xFFFF, interrupt_mask).unwrap();
        bus.write(target_address, 0b0000_0100).unwrap(); // inc b in handler

        let mut cpu = cpu(bus, state(0, 0, 0, 0, 2, 0x0100, false, true));

        step(&mut cpu, 2);

        assert_cpu(
            6,
            state(0, 0x0100, 0, 0, 0, target_address + 1, false, false),
            &cpu,
        );
        assert_eq!(cpu.bus[0xFF0F], 0);
        assert_eq!(cpu.bus[0], 0x00);
        assert_eq!(cpu.bus[1], 0x01);
    }
}

#[test]
fn interrupt_priority() {
    let mut bus = prog([0b1111_1011]); // ei
    ensure_size(&mut bus, 0x1_0000);

    bus.write(0xFF0F, 0b0001_1111).unwrap(); // prepare all interrupts
    bus.write(0xFFFF, 0b0001_1111).unwrap();
    bus.write(0x40, 0b1111_1011).unwrap(); // ie all handlers
    bus.write(0x48, 0b1111_1011).unwrap(); // ie all handlers
    bus.write(0x50, 0b1111_1011).unwrap(); // ie all handlers
    bus.write(0x58, 0b1111_1011).unwrap(); // ie all handlers
    bus.write(0x60, 0b1111_1011).unwrap(); // ie all handlers

    let mut cpu = cpu(bus, state(0, 0, 0, 0, 10, 0x0100, false, false));

    step(&mut cpu, 3);
    assert_eq!(cpu.reg.prog_counter, 0x0040);
    assert_eq!(cpu.bus[0xFF0F], 0b0001_1110);
    step(&mut cpu, 3);
    assert_eq!(cpu.reg.prog_counter, 0x0048);
    assert_eq!(cpu.bus[0xFF0F], 0b0001_1100);
    step(&mut cpu, 3);
    assert_eq!(cpu.reg.prog_counter, 0x0050);
    assert_eq!(cpu.bus[0xFF0F], 0b0001_1000);
    step(&mut cpu, 3);
    assert_eq!(cpu.reg.prog_counter, 0x0058);
    assert_eq!(cpu.bus[0xFF0F], 0b0001_0000);
    step(&mut cpu, 3);
    assert_eq!(cpu.reg.prog_counter, 0x0060);
    assert_eq!(cpu.bus[0xFF0F], 0b0000_0000);
}
