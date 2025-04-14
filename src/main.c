#include <stdio.h>

#include "boii/cartridge.h"
#include "boii/common.h"

int main(int argc, char **argv) {
    if (argc < 2) {
        fprintf(stderr, "[ERR] Usage: %s <ROM-FILEPATH>\n", argv[0]);
        return ERR_INVALID;
    }    
    const char *rom_filepath = argv[1];

    Cartridge cart;
    int err = cart_create_from_file(rom_filepath, &cart);
    printf("rom = %p (%d)\n", cart.rom, cart.rom_size);
    printf("ram = %p (%d)\n", cart.ram, cart.ram_size);
    cart_free(cart);
    
    return err;
}
