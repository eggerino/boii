#include "boii/cartridge.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define NINTENDO_LOGO_PTR 0x0104
#define TITLE_PTR 0x0134
#define NEW_LICENSEE_CODE_PTR 0x0144
#define SGB_FLAG_PTR 0x0146
#define CARTRIDGE_TYPE_PTR 0x0147
#define ROM_SIZE_PTR 0x0148
#define RAM_SIZE_PTR 0x0149
#define DESTINATION_CODE_PTR 0x014A
#define OLD_LICENSEE_CODE_PTR 0x014B
#define ROM_VERSION_PTR 0x014C
#define HEADER_CHECKSUM_PTR 0x014D
#define GLOBAL_CHECKSUM_PTR 0x014E

#define HEADER_SIZE 0x014F
#define TITLE_SIZE 0x10
#define NEW_LICENSEE_CODE_SIZE 0x02
#define NINTENDO_LOGO_SIZE 0x30

static int load_rom_from_file(const char *filepath);
static int parse_header(void);
static int validate_rom(void);
static int validate_nintendo_logo(void);
static int validate_header_checksum(void);
static int validate_global_checksum(void);
static int alloc_ram(void);

// Constants
static const u8 nintendo_logo[NINTENDO_LOGO_SIZE] = {
    0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D,
    0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99,
    0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E,
};

// Module state
static u8 *rom = NULL;
static u64 rom_size = 0;
static u8 *ram = NULL;
static u64 ram_size = 0;

static char title[TITLE_SIZE + 1] = {0};
static char new_licensee_code[NEW_LICENSEE_CODE_SIZE + 1] = {0};
static u8 sgb_flag = 0;
static u8 cartridge_type = 0;
static u8 destination_code = 0;
static u8 old_licensee_code = 0;
static u8 rom_version = 0;
static u8 header_checksum = 0;
static u16 global_checksum = 0;

int cart_init_from_file(const char *filepath) {
    int err = load_rom_from_file(filepath);
    if (err) {
        cart_deinit();
        return err;
    }

    err = parse_header();
    if (err) {
        cart_deinit();
        return err;
    }

    err = validate_rom();
    if (err) {
        cart_deinit();
        return err;
    }

    err = alloc_ram();
    if (err) {
        cart_deinit();
        return err;
    }

    return ERR_OK;
}

void cart_deinit(void) {
    free(rom);
    free(ram);
    rom = NULL;
    ram = NULL;
    rom_size = 0;
    ram_size = 0;
    memset(title, 0, TITLE_SIZE);
}

u8 cart_read(u16 address) {
    if (address < rom_size) {
        return rom[address];
    }
    
    fprintf(stderr, "[ERR] Cannot read from address %p in cartridge.\n", address);
    exit(ERR_SEGFAULT);
    return 0;
}

void cart_write(u16 address, u8 value) {
    fprintf(stderr, "[ERR] Cannot write to address %p in cartridge.\n", address);
    exit(ERR_SEGFAULT);
}

int load_rom_from_file(const char *filepath) {
    FILE *f = fopen(filepath, "rb");
    if (!f) {
        fprintf(stderr, "[ERR] Cannot open file %s.\n", filepath);
        return ERR_INVALID;
    }
    
    fseek(f, 0, SEEK_END);
    rom_size = ftell(f);
    rom = malloc(rom_size);
    if (!rom) {
        fclose(f);
        fprintf(stderr, "[ERR] Cannot allocate buffer for rom data.\n");
        return ERR_ALLOC;
    }

    fseek(f, 0, SEEK_SET);
    fread(rom, rom_size, 1, f);
    fclose(f);
    return ERR_OK;
}

int parse_header(void) {
    if (rom_size < HEADER_SIZE) {
        fprintf(stderr, "[ERR] Rom does not have a header.\n");
        return ERR_INVALID;
    }

    memcpy(title, &rom[TITLE_PTR], TITLE_SIZE);
    memcpy(new_licensee_code, &rom[NEW_LICENSEE_CODE_PTR], NEW_LICENSEE_CODE_SIZE);
    sgb_flag = rom[SGB_FLAG_PTR];
    cartridge_type = rom[CARTRIDGE_TYPE_PTR];
    u8 rom_size_value = rom[ROM_SIZE_PTR];
    u8 ram_size_value = rom[RAM_SIZE_PTR];
    destination_code = rom[DESTINATION_CODE_PTR];
    old_licensee_code = rom[OLD_LICENSEE_CODE_PTR];
    rom_version = rom[ROM_VERSION_PTR];
    header_checksum = rom[HEADER_CHECKSUM_PTR];
    global_checksum = ((u16)rom[GLOBAL_CHECKSUM_PTR]) << 8 | rom[GLOBAL_CHECKSUM_PTR + 1];

    // Check for matching rom sizes
    u64 computed_rom_size;
    if (rom_size_value <= 8) {
        computed_rom_size = 32 * 1024 * (1 << rom_size_value);
    } else if (rom_size_value == 0x52) {
        computed_rom_size = 1.1 * 1024 * 1024;
    } else if (rom_size_value == 0x53) {
        computed_rom_size = 1.2 * 1024 * 1024;
    } else if (rom_size_value == 0x54) {
        computed_rom_size = 1.5 * 1024 * 1024;
    } else {
        fprintf(stderr, "[ERR] Unkown rom size value of %d.\n", rom_size_value);
        return ERR_INVALID;
    }

    if (rom_size != computed_rom_size) {
        fprintf(stderr, "[ERR] Mismatching rom size. File=%d Header=%d\n.", rom_size, computed_rom_size);
        return ERR_INVALID;
    }

    // Compute ram size in bytes
    if (ram_size_value == 0) {
        ram_size = 0;
    } else if (ram_size_value == 2) {
        ram_size = 8 * 1024;
    } else if (ram_size_value == 3) {
        ram_size = 32 * 1024;
    } else if (ram_size_value == 4) {
        ram_size = 128 * 1024;
    } else if (ram_size_value == 5) {
        ram_size = 64 * 1024;
    } else {
        fprintf(stderr, "[ERR] Unkown ram size value of %d.\n", ram_size_value);
        return ERR_INVALID;
    }

    return ERR_OK;
}

int validate_rom(void) {
    int err = validate_nintendo_logo();
    if (err) {
        return err;
    }

    err = validate_header_checksum();
    if (err) {
        return err;
    }

#ifdef VALIDATE_GLOBAL_CHECKSUM
    err = validate_global_checksum();
    if (err) {
        return err;
    }
#endif

    return ERR_OK;
}

int validate_nintendo_logo(void) {
    if (memcmp(nintendo_logo, &rom[NINTENDO_LOGO_PTR], NINTENDO_LOGO_SIZE)) {
        fprintf(stderr, "[ERR] Rom does not contain the nintendo logo.\n");
        return ERR_INVALID;
    }

    return ERR_OK;
}

int validate_header_checksum(void) {
    u8 checksum = 0;
    for (u64 address = 0x0134; address <= 0x014C; ++address) {
        checksum = checksum - rom[address] - 1;
    }

    if (header_checksum != checksum) {
        fprintf(stderr, "[ERR] Mismatching header checksum. Header=%d Computed=%d.\n", header_checksum, checksum);
        return ERR_INVALID;
    }

    return ERR_OK;
}

int validate_global_checksum(void) {
    u16 checksum = 0;
    for (u64 address = 0; address < rom_size; ++address) {
        checksum += rom[address];
    }

    checksum -= rom[GLOBAL_CHECKSUM_PTR];
    checksum -= rom[GLOBAL_CHECKSUM_PTR + 1];

    if (global_checksum != checksum) {
        fprintf(stderr, "[ERR] Mismatching global checksum. Header=%d Computed=%d.\n", global_checksum, checksum);
        return ERR_INVALID;
    }

    return ERR_OK;
}

int alloc_ram(void) {
    ram = malloc(ram_size);
    if (!ram) {
        fprintf(stderr, "[ERR] Cannot allocate buffer for ram data.\n");
        return ERR_ALLOC;
    }

    return ERR_OK;
}
