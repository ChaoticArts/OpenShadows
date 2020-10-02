using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenShadows.FileFormats.Text
{
	public static class LxtExtractor
	{
		public static List<Tuple<int, string>> ExtractTexts(Stream lxtStream, bool hasCorruptedHeader = false)
		{
			using var f = new BinaryReader(lxtStream);

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
				strings.Add(new Tuple<int, string>(i, ExtractString(f)));
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

		private static string ExtractString(BinaryReader br)
		{
			var sb = new StringBuilder();

			byte b;
			do
			{
				b = br.ReadByte();

				switch (b)
				{
					case 0x81:
						sb.Append("ü");
						break;

					case 0x9a:
						sb.Append("Ü");
						break;

					case 0x84:
						sb.Append("ä");
						break;

					case 0x8e:
						sb.Append("Ä");
						break;

					case 0x94:
						sb.Append("ö");
						break;

					case 0x99:
						sb.Append("Ö");
						break;

					case 0xe1:
						sb.Append("ß");
						break;

					default:
						sb.Append(Encoding.ASCII.GetString(new[] { b }));
						break;
				}
			}
			while (b != 0x00);

			return sb.ToString();
		}
	}
}
