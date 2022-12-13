using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenShadows.FileFormats.DatFiles
{
    public static class DatExtractor
    {
        public static T ExtractDatFile<T>(byte[] data) where T : DatFile, new()
        {
            using var br = new BinaryReader(new MemoryStream(data));

            T dat = new T();
            dat.Extract(br);
            return dat;
        }
    }
}
