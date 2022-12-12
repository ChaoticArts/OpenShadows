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
        public static List<Tuple<int, string>> ExtractRawTexts(byte[] data, bool hasCorruptedHeader = false)
        {
            using var f = new BinaryReader(new MemoryStream(data));

            var strings = new List<Tuple<int, string>>();

			int counter = 0;
			while (f.BaseStream.Position< f.BaseStream.Length) 
			{
                strings.Add(new Tuple<int, string>(counter, Utils.ExtractString(f)));
				counter++;
            }

            return strings;
        }

        public static List<Tuple<int, string>> ExtractTexts(byte[] data, bool hasCorruptedHeader = false)
		{
			using var f = new BinaryReader(new MemoryStream(data));

			if (!hasCorruptedHeader && !CheckSignature(f))
			{
				throw new InvalidDataException("Not a valid LXT file");
			}

			var strings = new List<Tuple<int, string>>();

			// skip unknown bytes
			f.ReadBytes(0x04);

			int numberOfStrings = f.ReadInt32();

			// skip empty bytes
			f.ReadBytes(0x0e);

			for (int i = 0; i < numberOfStrings; i++)
			{
				strings.Add(new Tuple<int, string>(i, Utils.ExtractString(f)));
			}

			return strings;
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
