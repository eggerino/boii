#include "boii/cartridge.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define NINTENDO_LOGO_PTR 0x0104
#define ROM_SIZE_PTR 0x0148
#define RAM_SIZE_PTR 0x0149
#define HEADER_CHECKSUM_PTR 0x014D
#define GLOBAL_CHECKSUM_PTR 0x014E

#define HEADER_SIZE 0x014F

#define NINTENDO_LOGO_SIZE 48
static u8 nintendo_logo[NINTENDO_LOGO_SIZE] = {
    0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D,
    0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99,
    0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E,
};

static int read_rom_from_file(const char *filepath, u8 **ptr, u64 *size);
static int validate_rom(const u8 *rom, u64 size);
static int check_minimum_rom_size(u64 size);
static int check_nintendo_logo(const u8 *rom);
static int check_rom_size(const u8 *rom, u64 size);
static int check_header_checksum(const u8 *rom);
static int check_global_checksum(const u8 *rom, u64 size);
static int alloc_ram(const u8 *rom, u8 **ptr, u64 *size);

int cart_create_from_file(const char *filepath, Cartridge *cart) {
    u8 *rom;
    u64 rom_size;
    int err = read_rom_from_file(filepath, &rom, &rom_size);
    if (err) {
        return err;
    }

    err = validate_rom(rom, rom_size);
    if (err) {
        free(rom);
        return err;
    }

    u8 *ram;
    u64 ram_size;
    err = alloc_ram(rom, &ram, &ram_size);
    if (err) {
        free(rom);
        return err;
    }

    cart->rom = rom;
    cart->rom_size = rom_size;
    cart->ram = ram;
    cart->ram_size = ram_size;
    return ERR_OK;
}

void cart_free(Cartridge cart) {
    free(cart.rom);
    free(cart.ram);
}

int read_rom_from_file(const char *filepath, u8 **ptr, u64 *size) {
    FILE *f = fopen(filepath, "rb");
    if (!f) {
        fprintf(stderr, "[ERR] Cannot open file %s.\n", filepath);
        return ERR_INVALID;
    }

    fseek(f, 0, SEEK_END);
    *size = ftell(f);
    *ptr = malloc(*size);

    if (!*ptr) {
        fclose(f);
        fprintf(stderr, "[ERR] Cannot allocate buffer for rom data.\n");
        return ERR_ALLOC;
    }

    fseek(f, 0, SEEK_SET);
    fread(*ptr, *size, 1, f);
    fclose(f);

    return ERR_OK;
}

int validate_rom(const u8 *rom, u64 size) {
    int err = check_minimum_rom_size(size);
    if (err) {
        return err;
    }

    err = check_nintendo_logo(rom);
    if (err) {
        return err;
    }

    err = check_rom_size(rom, size);
    if (err) {
        return err;
    }

    err = check_header_checksum(rom);
    if (err) {
        return err;
    }

#ifdef CHECK_GLOBAL_CHECKSUM
    err = check_global_checksum(rom, size);
    if (err) {
        return err;
    }
#endif

    return ERR_OK;
}

int check_minimum_rom_size(u64 size) {
    if (size < HEADER_SIZE) {
        fprintf(stderr, "[ERR] Rom does not have a header.\n");
        return ERR_INVALID;
    }
    return ERR_OK;
}

int check_nintendo_logo(const u8 *rom) {
    if (memcmp(nintendo_logo, &rom[NINTENDO_LOGO_PTR], NINTENDO_LOGO_SIZE)) {
        fprintf(stderr, "[ERR] Rom does not contain the nintendo logo.\n");
        return ERR_INVALID;
    }
    return ERR_OK;
}

int check_rom_size(const u8 *rom, u64 size) {
    u8 rom_size_value = rom[ROM_SIZE_PTR];

    u64 rom_size;
    if (rom_size_value <= 8) {
        rom_size = 32 * 1024 * (1 << rom_size_value);
    } else if (rom_size_value == 0x52) {
        rom_size = 1.1 * 1024 * 1024;
    } else if (rom_size_value == 0x53) {
        rom_size = 1.2 * 1024 * 1024;
    } else if (rom_size_value == 0x54) {
        rom_size = 1.5 * 1024 * 1024;
    } else {
        fprintf(stderr, "[ERR] Unkown rom size value of %d.\n", rom_size_value);
        return ERR_INVALID;
    }

    if (rom_size != size) {
        fprintf(stderr, "[ERR] Mismatching rom size. Header=%d File=%d\n.", rom_size, size);
        return ERR_INVALID;
    }
    return ERR_OK;
}

int check_header_checksum(const u8 *rom) {
    u8 header_checksum = rom[HEADER_CHECKSUM_PTR];

    u8 checksum = 0;
    for (u16 address = 0x0134; address <= 0x014C; ++address) {
        checksum = checksum - rom[address] - 1;
    }

    if (header_checksum != checksum) {
        fprintf(stderr, "[ERR] Mismatching header checksum. Header=%d Computed=%d.\n", header_checksum, checksum);
        return ERR_INVALID;
    }

    return ERR_OK;
}

int check_global_checksum(const u8 *rom, u64 size) {
    u16 global_checksum = rom[GLOBAL_CHECKSUM_PTR];
    global_checksum = global_checksum << 8 | rom[GLOBAL_CHECKSUM_PTR + 1];

    u16 checksum = 0;
    for (u64 address = 0; address < size; ++address) {
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

int alloc_ram(const u8 *rom, u8 **ptr, u64 *size) {
    u8 ram_size_value = rom[RAM_SIZE_PTR];

    if (ram_size_value == 0) {
        *size = 0;
        *ptr = NULL;
        return ERR_OK;
    } else if (ram_size_value == 2) {
        *size = 8 * 1024;
    } else if (ram_size_value == 3) {
        *size = 32 * 1024;
    } else if (ram_size_value == 4) {
        *size = 128 * 1024;
    } else if (ram_size_value == 5) {
        *size = 64 * 1024;
    } else {
        fprintf(stderr, "[ERR] Unkown ram size value of %d.\n", ram_size_value);
        return ERR_INVALID;
    }

    *ptr = malloc(*size);
    if (!ptr) {
        fprintf(stderr, "[ERR] Cannot allocate buffer for ram data.\n");
        return ERR_ALLOC;
    }
    return ERR_OK;
}
