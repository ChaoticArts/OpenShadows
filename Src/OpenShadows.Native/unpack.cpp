#include "unpack.h"

#include <vector>
#include "pp20.h"

int __stdcall unpack_pp20(unsigned char* in_data, const int in_size, unsigned char* out_data, const int out_size)
{
	pp20::unpack(in_data, in_size, out_data, out_size);

	return 0;
}

int __stdcall unpack_bopa(char* in_data, const int in_size, char* out_data, const int out_size)
{
	unsigned int size         = static_cast<unsigned int>(in_size);
	unsigned int raw_offset   = 0;
	unsigned int bopa_offset  = 0;
	unsigned int current_size = in_size;

	unsigned int c = 0;
	char flags[] = {
		static_cast<char>(0x80),
		static_cast<char>(0x40),
		static_cast<char>(0x20),
		static_cast<char>(0x10),
		static_cast<char>(0x08),
		static_cast<char>(0x04),
		static_cast<char>(0x02),
		static_cast<char>(0x01)
	};

	bool running = true;

	std::vector<char> raw_data;
	raw_data.resize(out_size, 0x00);

	while ((c < current_size) && running)
	{
		char code = in_data[bopa_offset++];

		if (bopa_offset >= 0x3f1)
		{
			int ii = 0;
		}

		if (code == '\xff')
		{
			// direct copy for 8 bytes
			std::memcpy(&raw_data[raw_offset], &in_data[bopa_offset], 8);
			bopa_offset += 8;
			raw_offset += 8;
			c += 8;
		}
		else
		{
			for (unsigned int f = 0; f < 8; ++f)
			{
				if ((code & flags[f]) != 0)
				{
					// direct copy for 1 byte
					std::memcpy(&raw_data[raw_offset], &in_data[bopa_offset], 1);
					bopa_offset += 1;
					raw_offset += 1;
					c += 1;
				}
				else
				{
					if (bopa_offset >= size)
					{
						// nothing more to read
						running = false;
						break;
					}

					// reuse sequence of bytes
					char subcode = in_data[bopa_offset++];

					unsigned int count = subcode & '\x0f';
					unsigned char offset1 = static_cast<unsigned char>(subcode & '\xf0') >> 4;
					unsigned char offset2 = static_cast<unsigned char>(in_data[bopa_offset++]);
					unsigned int  offset = offset2 + (offset1 << 8);

					count += 3;
					c += 2;

					if (count > offset)
					{
						unsigned int from = raw_offset - offset;
						auto start = raw_data.begin() + from;
						auto end = raw_data.begin() + raw_offset;
						std::vector<char> sub(start, end);

						for (unsigned int i = 0; i < count; ++i)
						{
							unsigned int ii = i;
							if (ii >= sub.size())
							{
								ii %= sub.size();
							}
							std::memcpy(&raw_data[raw_offset], &sub[ii], 1);
							raw_offset += 1;
						}
					}
					else
					{
						unsigned int from = raw_offset - offset;
						unsigned int to = from + count;

						auto start = raw_data.begin() + from;
						auto end = raw_data.begin() + to;
						std::vector<char> sub(start, end);

						std::memcpy(&raw_data[raw_offset], sub.data(), sub.size());
						raw_offset += static_cast<unsigned int>(sub.size());
					}
				}
			}
		}
	}

	std::memcpy(out_data, raw_data.data(), out_size);
	
	return 0;
}
