#include "boii/bus.h"

#include <stdio.h>
#include <stdlib.h>

#include "boii/cartridge.h"

u8 bus_read(u16 address) {
    if (address < 0x8000) {
        return cart_read(address);
    }

    fprintf(stderr, "[ERR] Bus segfault. Unable to read from address %p.\n", address);
    exit(ERR_SEGFAULT);
    return 0;
}

void bus_write(u16 address, u8 value) {
    if (address < 0x8000) {
        cart_write(address, value);
        return;
    }

    fprintf(stderr, "[ERR] Bus segfault. Unable to write to address %p.\n", address);
    exit(ERR_SEGFAULT);
}
