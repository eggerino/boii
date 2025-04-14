#include <stdio.h>

#include "boii/cartridge.h"
#include "boii/common.h"


int main(int argc, char **argv) {
#ifndef DEBUG_FILE
    if (argc < 2) {
        fprintf(stderr, "[ERR] Usage: %s <ROM-FILEPATH>\n", argv[0]);
        return ERR_INVALID;
    }    
    const char *rom_filepath = argv[1];
    int err = cart_init_from_file(rom_filepath);
#else
    int err = cart_init_from_file(DEBUG_FILE);
#endif

    cart_deinit();
    
    return err;
}
