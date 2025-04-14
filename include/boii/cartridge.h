#ifndef BOII_CARTRIDGE_H_
#define BOII_CARTRIDGE_H_

#include "boii/common.h"

typedef struct {
    u8 *rom;
    u64 rom_size;

    u8 *ram;
    u64 ram_size;
} Cartridge;

int cart_create_from_file(const char *filepath, Cartridge *cart);
void cart_free(Cartridge cart);

#endif
