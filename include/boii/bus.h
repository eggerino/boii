#ifndef BOII_BUS_H_
#define BOII_BUS_H_

#include "boii/common.h"

u8 bus_read(u16 address);
void bus_write(u16 address, u8 value);

#endif
