using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenShadows.FileFormats.Text
{
    public static class NeiExtractor
    {
        public static string[] ExtractNei(byte[] data)
        {
            using var f = new BinaryReader(new MemoryStream(data));

            byte[] uncompressedData;
            if (Utils.IsBoPaCompressed(data))
            {
                // skip
                f.ReadBytes(0x0a);

                // unpack BoPa
                uint uncompressedSize = Utils.SwapEndianess(f.ReadUInt32());
                uint compressedSize = Utils.SwapEndianess(f.ReadUInt32());
                uncompressedData = new byte[uncompressedSize];
                var compressedData = f.ReadBytes((int)compressedSize);

                Utils.UnpackBoPa(compressedData, compressedSize, uncompressedData, uncompressedSize);
            }
            else
            {
                uncompressedData = data;
            }

            int position = 0;
            List<string> result = new List<string>();
            while (position < uncompressedData.Length) 
            {
                result.Add(ReadBlock(uncompressedData, ref position));
            }
            return result.ToArray();
        }

        private static string ReadBlock(byte[] uncompressedData, ref int position)
        {
            StringBuilder sb = new StringBuilder();
            while (position < uncompressedData.Length)
            {
                byte b = uncompressedData[position++];
                if (b == 0x0D)
                {
                    if (position < uncompressedData.Length - 1)
                    {
                        b = uncompressedData[position++];
                        if (b == 0x0A)
                        {
                            break;
                        }
                    }
                }
                sb.Append((char)b);
            }
            return sb.ToString();
        }
    }
}
