#ifndef PP20_H
#define PP20_H

/*
    Modified code from OpenMPT project, licenced under the BSD.
*/

template<class T, class C>
inline void limit_max(T& val, const C upper_limit)
{
    if (val > upper_limit)
    {
        val = upper_limit;
    }
}


class pp20
{
public:
    static bool unpack(const unsigned char* src, unsigned int nSrcLen, unsigned char* pDst, unsigned int nDstLen);

private:
    unsigned int get_bits(unsigned int n);

    unsigned int bitcount;
    unsigned int bitbuffer;
    const unsigned char* start;
    const unsigned char* src;
};

#endif
