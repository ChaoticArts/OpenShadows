#pragma once

extern "C"
{
	__declspec(dllexport) int __stdcall unpack_pp20(unsigned char* in_data, int in_size, unsigned char* out_data, int out_size);

	__declspec(dllexport) int __stdcall unpack_bopa(char* in_data, int in_size, char* out_data, int out_size);
}
