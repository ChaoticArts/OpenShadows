using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenShadows.FileFormats.DatFiles
{
    public class DatMapIconPosition : DatFile
    {
        public struct IconPosition
        {
            public ushort X;
            public ushort Y;
        }

        public IconPosition[] Icons = Array.Empty<IconPosition>();

        internal override bool Extract(BinaryReader reader)
        {
            var str = Utils.ExtractString(reader);
            if (str != "AMAPICONPOS")
            {
                throw new InvalidDataException("Not a valid AMAPICONPOS file");
            }
            str = Utils.ExtractString(reader);
            if (str != "ARR")
            {
                throw new InvalidDataException("Not a valid AMAPICONPOS file");
            }
            ushort len = reader.ReadUInt16();
            Icons = new IconPosition[len / 4];
            for (int i = 0; i < Icons.Length; i++)
            {
                Icons[i] = new IconPosition()
                { 
                    X = reader.ReadUInt16(),
                    Y = reader.ReadUInt16(),
                };
            }

            return true;
        }
    }
}
