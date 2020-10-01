#include "pp20.h"

#include <iostream>

/*
Modified code from OpenMPT project, licenced under the BSD.
*/

unsigned int pp20::get_bits(unsigned int n)
{
    unsigned int result = 0;

    for (unsigned int i = 0; i < n; i++)
    {
        if (!bitcount)
        {
            bitcount = 8;
            if (src != start) 
            {
                src--;
            }
            bitbuffer = *src;
        }
        result = (result << 1) | (bitbuffer & 1);
        bitbuffer >>= 1;
        bitcount--;
    }
    return result;
}

bool pp20::unpack(const unsigned char* source, unsigned int src_size, unsigned char* destination, unsigned int dst_size)
{
    pp20 bit_buffer;
    unsigned int bytes_left;

    bit_buffer.start = source;
    bit_buffer.src = source + src_size - 4;
    bit_buffer.bitbuffer = 0;
    bit_buffer.bitcount = 0;
    bit_buffer.get_bits(source[src_size - 1]);
    bytes_left = dst_size;

    while (bytes_left > 0)
    {
        if (!bit_buffer.get_bits(1))
        {
            unsigned int n = 1;
            while (n < bytes_left)
            {
                unsigned int code = bit_buffer.get_bits(2);
                n += code;
                if (code != 3) break;
            }
            limit_max(n, bytes_left);
            for (unsigned int i = 0; i<n; i++)
            {
                destination[--bytes_left] = (unsigned char)bit_buffer.get_bits(8);
            }
            if (!bytes_left) break;
        }
        {
            unsigned int n = bit_buffer.get_bits(2) + 1;
            if (n < 1 || n - 1 >= src_size) return false;
            unsigned int nbits = source[n - 1];
            unsigned int nofs;
            if (n == 4)
            {
                nofs = bit_buffer.get_bits((bit_buffer.get_bits(1)) ? nbits : 7);
                while (n < bytes_left)
                {
                    unsigned int code = bit_buffer.get_bits(3);
                    n += code;
                    if (code != 7) break;
                }
            }
            else
            {
                nofs = bit_buffer.get_bits(nbits);
            }
            limit_max(n, bytes_left);
            for (unsigned int i = 0; i <= n; i++)
            {
                destination[bytes_left - 1] = (bytes_left + nofs < dst_size) ? destination[bytes_left + nofs] : 0;
                if (!--bytes_left) break;
            }
        }
    }
    return true;
}
