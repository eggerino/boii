#ifndef BOII_CARTRIDGE_H_
#define BOII_CARTRIDGE_H_

#include "boii/common.h"

int cart_init_from_file(const char *filepath);
void cart_deinit(void);

u8 cart_read(u16 address);
void cart_write(u16 address, u8 value);

#endif
