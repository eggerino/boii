
// Fixed addresses in the cartridge
const ENTRY_POINT: usize = 0x0100;
const NINTENDO_LOGO: usize = 0x0104;
const ITILE: usize = 0x0134;
const CGB_FLAG: usize = 0x0143;
const NEW_LICENSEE_CODE: usize = 0x0144;
const SGB_FLAG: usize = 0x0146;
const CARTRIDGE_TYPE: usize = 0x0147;
const ROM_SIZE: usize = 0x0148;
const RAM_SIZE: usize = 0x0149;
const DESTINATION_CODE: usize = 0x014A;
const OLD_LICENSEE_CODE: usize = 0x014B;
const ROM_VERSION: usize = 0x014C;
const HEADER_CHECKSUM: usize = 0x014D;
const GLOBAL_CHECKSUM: usize = 0x014E;

const HEADER_SIZE: usize = 0x0150;
const TITLE_SIZE: usize = 0x0010;
const NEW_LICENSEE_CODE_SIZE: usize = 0x0002;

const NINTENDO_LOGO_LITERAL: [usize; 0x0030] = [
    0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D,
    0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99,
    0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E,
];

enum CgbFlag {
    Color(u8),
    Monochrom(u8),
}

enum SgbFlag {
    Use,
    Ignore(u8),
}

enum MemoryBlockControllerType {
    None,
    One,
    Two,
    Three,
    Five,
    Six,
    Seven,
}

struct CartridgeType {
    mbc: MemoryBlockControllerType,
    ram: bool,
    battery: bool,
    mmm01: bool,
    timer: bool,
    rumble: bool,
    sensor: bool,
    pocket_camera: bool,
    bandai_tama5: bool,
    huc1: bool,
    huc3: bool,
}

enum DestinationCode {
    Japan,
    Oversea,
}

struct Header {
    title: String,
    cgb_flag: CgbFlag,
    licensee: String,
    sgb_flag: SgbFlag,
    cartridge_type: CartridgeType,
    rom_size: usize,
    ram_size: usize,
    destination_code: DestinationCode,
    rom_version: u8,
    header_checksum: u8,
    global_checksum: u16,
}
