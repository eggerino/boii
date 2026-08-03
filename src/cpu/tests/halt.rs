use super::*;

#[test]
fn cpu_halts_until_interrupt() {
    let mut bus = prog([0b0111_0110]); // halt
    ensure_size(&mut bus, 0x1_0000);
    let mut cpu = cpu(bus, state(0, 0, 0, 0, 2, 0x0100, false, true));

    step(&mut cpu, 2);
    assert_cpu(2, state(0, 0, 0, 0, 2, 0x0101, true, true), &cpu);

    cpu.bus.write(0xFF0F, 0b0001_1111).unwrap();
    cpu.bus.write(0xFFFF, 0b0001_1111).unwrap();

    cpu.step().unwrap();
    assert_cpu(7, state(0, 0, 0, 0, 0, 0x0040, false, false), &cpu);
    assert_eq!(cpu.bus[0], 0x01);
    assert_eq!(cpu.bus[1], 0x01);
}

#[test]
fn cpu_wakes_on_interrupt() {
    let mut bus = prog([0b0111_0110]); // halt
    ensure_size(&mut bus, 0x1_0000);
    let mut cpu = cpu(bus, state(0, 0, 0, 0, 0, 0x0100, false, false));

    step(&mut cpu, 2);
    assert_cpu(2, state(0, 0, 0, 0, 0, 0x0101, true, false), &cpu);

    cpu.bus.write(0xFF0F, 0b0001_1111).unwrap();
    cpu.bus.write(0xFFFF, 0b0001_1111).unwrap();

    cpu.step().unwrap();
    assert_cpu(3, state(0, 0, 0, 0, 0, 0x0102, false, false), &cpu);
}

#[test]
fn halt_bug_reread_next_byte() {
    let mut bus = prog([
        0b0111_0110, // halt
        0x06,
        0x04, // ld b, 4
    ]);
    ensure_size(&mut bus, 0x1_0000);
    bus.write(0xFF0F, 0b0001_1111).unwrap();
    bus.write(0xFFFF, 0b0001_1111).unwrap();

    let mut cpu = Cpu::new(bus);

    step(&mut cpu, 3);

    // Cpu sees:
    //      halt
    //      ld b, 6
    //      inc b
    assert_cpu(4, state(0, 0x0700, 0, 0, 0, 0x0103, false, false), &cpu);
}

#[test]
fn halt_bug_halt_can_loop() {
    let mut bus = prog([
        0b0111_0110, // halt
        0b0111_0110, // halt
    ]);
    ensure_size(&mut bus, 0x1_0000);
    bus.write(0xFF0F, 0b0001_1111).unwrap();
    bus.write(0xFFFF, 0b0001_1111).unwrap();

    let mut cpu = Cpu::new(bus);

    step(&mut cpu, 10);

    assert_cpu(10, state(0, 0, 0, 0, 0, 0x0101, false, false), &cpu);
}

#[test]
fn halt_bug_enable_interrupt_halt_will_return_to_halt() {
    let mut bus = prog([
        0b1111_1011, // ei
        0b0111_0110, // halt
    ]);
    ensure_size(&mut bus, 0x1_0000);
    bus.write(0xFF0F, 0b0000_0001).unwrap();
    bus.write(0xFFFF, 0b0000_0001).unwrap();
    bus.write(0x0040, 0b1100_1001).unwrap(); // Return the interrupt handler immediately
    let mut cpu = cpu(bus, state(0, 0, 0, 0, 2, 0x0100, false, false));

    step(&mut cpu, 4);
    assert_cpu(11, state(0, 0, 0, 0, 2, 0x0101, false, false), &cpu);
    assert_eq!(cpu.bus[0], 0x01);
    assert_eq!(cpu.bus[1], 0x01);
}
