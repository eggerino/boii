use super::*;

#[test]
fn load_literal8() {
    let bus = prog([
        0b0011_0110,
        0xFF, // ld [hl], 256 (ld [0], 256)
        0b0000_0110,
        0x01, // ld b, 1
        0b0000_1110,
        0x02, // ld c, 2
        0b0001_0110,
        0x03, // ld d, 3
        0b0001_1110,
        0x04, // ld e, 4
        0b0010_0110,
        0x05, // ld h, 5
        0b0010_1110,
        0x06, // ld l, 6
        0b0011_1110,
        0x08, // ld a, 8
    ]);
    let mut cpu = Cpu::new(bus);

    step(&mut cpu, 8);

    assert_cpu(
        17,
        state(0x0800, 0x0102, 0x0304, 0x0506, 0x0000, 0x0110, false, false),
        &cpu,
    );
    assert_eq!(cpu.bus[0], 0xFF);
}

#[test]
fn load_register8_to_register8_from_b() {
    let bus = prog([
        0b0111_0000, // ld [hl], b
        0b0100_0000, // ld b, b
        0b0100_1000, // ld c, b
        0b0101_0000, // ld d, b
        0b0101_1000, // ld e, b
        0b0110_0000, // ld h, b
        0b0110_1000, // ld l, b
        0b0111_1000, // ld a, b
    ]);
    let mut cpu = cpu(bus, state(0, 0x0100, 0, 0, 0, 0x0100, false, false));

    step(&mut cpu, 8);

    assert_cpu(
        9,
        state(0x0100, 0x0101, 0x0101, 0x0101, 0, 0x0108, false, false),
        &cpu,
    );
    assert_eq!(cpu.bus[0], 1);
}

#[test]
fn load_register8_to_register8_into_b() {
    let bus = prog([
        // 0b0100_0110,        // ld b, [hl]
        0b0100_0000, // ld b, b
        0b0100_0001, // ld b, c
        0b0100_0010, // ld b, d
        0b0100_0011, // ld b, e
        0b0100_0100, // ld b, h
        0b0100_0101, // ld b, l
        0b0100_0111, // ld b, a
    ]);
    let mut cpu = cpu(
        bus,
        state(0x0600, 0x001, 0x0203, 0x0405, 0, 0x0100, false, false),
    );

    cpu.step().unwrap();
    assert_cpu(
        1,
        state(0x0600, 0x0001, 0x0203, 0x0405, 0, 0x0101, false, false),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        2,
        state(0x0600, 0x0101, 0x0203, 0x0405, 0, 0x0102, false, false),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        3,
        state(0x0600, 0x0201, 0x0203, 0x0405, 0, 0x0103, false, false),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        4,
        state(0x0600, 0x0301, 0x0203, 0x0405, 0, 0x0104, false, false),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        5,
        state(0x0600, 0x0401, 0x0203, 0x0405, 0, 0x0105, false, false),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        6,
        state(0x0600, 0x0501, 0x0203, 0x0405, 0, 0x0106, false, false),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        7,
        state(0x0600, 0x0601, 0x0203, 0x0405, 0, 0x0107, false, false),
        &cpu,
    );
}

#[test]
fn load_register8_to_register8_hlpointer_into_b() {
    let mut bus = prog([
        0b0100_0110, // ld b, [hl]
    ]);
    bus.write(0, 1).unwrap();
    let mut cpu = Cpu::new(bus);

    cpu.step().unwrap();
    assert_cpu(2, state(0, 0x0100, 0, 0, 0, 0x0101, false, false), &cpu);
}

#[test]
fn load_literal16() {
    let bus = prog([
        0b0000_0001,
        0x01,
        0x02, // ld bc, 0x0201
        0b0001_0001,
        0x03,
        0x04, // ld de, 0x0403
        0b0010_0001,
        0x05,
        0x06, // ld hl, 0x0605
        0b0011_0001,
        0x07,
        0x08, // ld sp, 0x0807
    ]);
    let mut cpu = Cpu::new(bus);

    step(&mut cpu, 4);

    assert_cpu(
        12,
        state(0, 0x0201, 0x0403, 0x0605, 0x0807, 0x010C, false, false),
        &cpu,
    );
}

#[test]
fn load_from_a() {
    let bus = prog([
        0b0000_0010, // ld [bc], a
        0b0001_0010, // ld [de], a
        0b0010_0010, // ld [hl+], a
        0b0011_0010, // ld [hl-], a
    ]);
    let mut cpu = cpu(
        bus,
        state(0xFF00, 0x0005, 0x0006, 0, 0, 0x0100, false, false),
    );

    step(&mut cpu, 4);

    assert_cpu(
        8,
        state(0xFF00, 0x0005, 0x0006, 0x0000, 0, 0x0104, false, false),
        &cpu,
    );
    assert_eq!(cpu.bus[0x0005], 0xFF);
    assert_eq!(cpu.bus[0x0006], 0xFF);
    assert_eq!(cpu.bus[0x0000], 0xFF);
    assert_eq!(cpu.bus[0x0001], 0xFF);
}

#[test]
fn load_from_a_into_literal16_pointer() {
    let bus = prog([0b1110_1010, 0x05, 0x00]); // ld [5], a
    let mut cpu = cpu(bus, state(0x0400, 0, 0, 0, 0, 0x0100, false, false));

    cpu.step().unwrap();

    assert_cpu(4, state(0x0400, 0, 0, 0, 0, 0x0103, false, false), &cpu);
    assert_eq!(cpu.bus[0x5], 4);
}

#[test]
fn load_from_a_into_literal8_high_pointer() {
    let mut bus = prog([0b1110_0000, 0x01]); // ldh [1], a
    ensure_size(&mut bus, 0xFF02);
    let mut cpu = cpu(bus, state(0x0400, 0, 0, 0, 0, 0x0100, false, false));

    cpu.step().unwrap();

    assert_cpu(3, state(0x0400, 0, 0, 0, 0, 0x0102, false, false), &cpu);
    assert_eq!(cpu.bus[0xFF01], 4);
}

#[test]
fn load_from_a_into_c_high_pointer() {
    let mut bus = prog([0b1110_0010]); // ldh [c], a
    ensure_size(&mut bus, 0xFF02);
    let mut cpu = cpu(bus, state(0x0400, 0x0001, 0, 0, 0, 0x0100, false, false));

    cpu.step().unwrap();

    assert_cpu(
        2,
        state(0x0400, 0x0001, 0, 0, 0, 0x0101, false, false),
        &cpu,
    );
    assert_eq!(cpu.bus[0xFF01], 4);
}

#[test]
fn load_into_a() {
    let mut bus = prog([
        0b0000_1010, // ld a, [bc]
        0b0001_1010, // ld a, [de]
        0b0010_1010, // ld a, [hl+]
        0b0011_1010, // ld a, [hl-]
    ]);
    bus.write(1, 1).unwrap();
    bus.write(2, 2).unwrap();
    bus.write(3, 3).unwrap();
    bus.write(4, 4).unwrap();

    let mut cpu = cpu(bus, state(0, 1, 2, 3, 0, 0x0100, false, false));

    cpu.step().unwrap();
    assert_cpu(
        2,
        state(0x0100, 0x0001, 0x0002, 0x0003, 0, 0x0101, false, false),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        4,
        state(0x0200, 0x0001, 0x0002, 0x0003, 0, 0x0102, false, false),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        6,
        state(0x0300, 0x0001, 0x0002, 0x0004, 0, 0x0103, false, false),
        &cpu,
    );
    cpu.step().unwrap();
    assert_cpu(
        8,
        state(0x0400, 0x0001, 0x0002, 0x0003, 0, 0x0104, false, false),
        &cpu,
    );
}

#[test]
fn load_from_literal16_pointer_into_a() {
    let mut bus = prog([0b1111_1010, 0x05, 0x00]); // ld [5], a
    bus.write(0x5, 4).unwrap();
    let mut cpu = cpu(bus, state(0, 0, 0, 0, 0, 0x0100, false, false));

    cpu.step().unwrap();

    assert_cpu(4, state(0x0400, 0, 0, 0, 0, 0x0103, false, false), &cpu);
}

#[test]
fn load_from_literal8_high_pointer_into_a() {
    let mut bus = prog([0b1111_0000, 0x01]); // ldh [1], a
    ensure_size(&mut bus, 0xFF02);
    bus.write(0xFF01, 4).unwrap();
    let mut cpu = cpu(bus, state(0, 0, 0, 0, 0, 0x0100, false, false));

    cpu.step().unwrap();

    assert_cpu(3, state(0x0400, 0, 0, 0, 0, 0x0102, false, false), &cpu);
}

#[test]
fn load_from_c_high_pointer_into_a() {
    let mut bus = prog([0b1111_0010]); // ldh a, [c]
    ensure_size(&mut bus, 0xFF02);
    bus.write(0xFF01, 4).unwrap();
    let mut cpu = cpu(bus, state(0, 0x0001, 0, 0, 0, 0x0100, false, false));

    cpu.step().unwrap();

    assert_cpu(
        2,
        state(0x0400, 0x0001, 0, 0, 0, 0x0101, false, false),
        &cpu,
    );
}
