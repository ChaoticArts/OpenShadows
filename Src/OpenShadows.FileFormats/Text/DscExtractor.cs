using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenShadows.FileFormats.Text
{
    public static class DscExtractor
    {
        public static string ExtractDsc(byte[] data)
        {
            using var f = new BinaryReader(new MemoryStream(data));

            // skip
            f.ReadBytes(0x0a);

            // unpack BoPa
            uint uncompressedSize = Utils.SwapEndianess(f.ReadUInt32());
            uint compressedSize = Utils.SwapEndianess(f.ReadUInt32());
            var uncompressedData = new byte[uncompressedSize];
            var compressedData = f.ReadBytes((int)compressedSize);

            Utils.UnpackBoPa(compressedData, compressedSize, uncompressedData, uncompressedSize);

            using var f2 = new BinaryReader(new MemoryStream(uncompressedData));
            return Utils.ExtractString(f2);
        }
    }
}
