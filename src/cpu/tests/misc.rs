use super::*;

#[test]
fn nop() {
    let bus = prog([0x00]); // nop
    let mut cpu = Cpu::new(bus);

    step(&mut cpu, 1);

    assert_cpu(1, state(0, 0, 0, 0, 0, 0x0101, false, false), &cpu);
}

#[test]
fn decimal_adjust_accumulator() {
    let data = [
        // Subtraction cases
        (0x6600 | 0b0100_0000, 0x6600 | 0b0100_0000),
        (0x6600 | 0b0110_0000, 0x6000 | 0b0100_0000),
        (0x6600 | 0b0101_0000, 0x0600 | 0b0101_0000),
        (0x6600 | 0b0111_0000, 0x0000 | 0b1101_0000),
        // Addition cases
        (0x0000 | 0b0000_0000, 0x0000 | 0b1000_0000),
        (0x0000 | 0b0010_0000, 0x0600 | 0b0000_0000),
        (0x0A00 | 0b0000_0000, 0x1000 | 0b0000_0000),
        (0x0000 | 0b0001_0000, 0x6000 | 0b0001_0000),
        (0xA000 | 0b0000_0000, 0x0000 | 0b1001_0000),
        (0x0000 | 0b0011_0000, 0x6600 | 0b0001_0000),
    ];

    for (initial_af, expected_af) in data {
        let bus = prog([0b0010_0111]); // daa
        let mut cpu = cpu(bus, state(initial_af, 0, 0, 0, 0, 0x0100, false, false));

        cpu.step().unwrap();

        assert_cpu(
            1,
            state(expected_af, 0, 0, 0, 0, 0x0101, false, false),
            &cpu,
        );
    }
}
