using OpenShadows.Data.Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenShadows.FileFormats.Text
{
	public static class LxtExtractor
	{
		/// <summary>
		/// Very special handling for raw text files (like ITEMNAME.LXT, PRINTER.LXT) which 
		/// have no real structure whatsoever. (just an array of zero-terminated strings)
		/// </summary>
        public static StringTable ExtractRawStringTable(byte[] data)
        {
            using var f = new BinaryReader(new MemoryStream(data));

            var strings = new List<string>();

			int counter = 0;
			while (f.BaseStream.Position< f.BaseStream.Length) 
			{
                strings.Add(Utils.ExtractString(f));
				counter++;
            }

            return new StringTable(strings.ToArray());
        }

        public static StringTable ExtractStringTable(byte[] data)
		{
			using var f = new BinaryReader(new MemoryStream(data));

			if (!CheckSignature(f))
			{
				throw new InvalidDataException("Not a valid LXT file");
			}

			var strings = new List<string>();

			// skip unknown bytes
			f.ReadBytes(0x04);

			int numberOfStrings = f.ReadInt32();

			// skip empty bytes
			f.ReadBytes(0x0e);

			for (int i = 0; i < numberOfStrings; i++)
			{
				strings.Add(Utils.ExtractString(f));
			}

			return new StringTable(strings.ToArray());
        }

		private static bool CheckSignature(BinaryReader br)
		{
			byte l = br.ReadByte();
			byte x = br.ReadByte();
			byte t = br.ReadByte();
			byte s = br.ReadByte();

			return l == 0x4c && x == 0x58 && t == 0x54 && s == 0x20;
		}
	}
}
