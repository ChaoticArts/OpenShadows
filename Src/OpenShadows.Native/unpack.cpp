#include "unpack.h"

#include "pp20.h"

int __stdcall unpack_pp20(unsigned char* in_data, const int in_size, unsigned char* out_data, const int out_size)
{
    pp20::unpack(in_data, in_size, out_data, out_size);

    return 0;
}
