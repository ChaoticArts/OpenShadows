using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenShadows.FileFormats.Text
{
	public static class XdfExtractor
	{
		public static List<Tuple<int, string>> ExtractTexts(Stream xdfStream)
		{
			using var f = new BinaryReader(xdfStream);

			if (!CheckSignature(f))
			{
				throw new InvalidDataException("Not a valid XDF file");
			}

			var strings = new List<Tuple<int, string>>();

			f.ReadBytes(0x20);

			int offsetOfStrings = f.ReadInt32();
			f.BaseStream.Seek(offsetOfStrings, SeekOrigin.Begin);

			for (int i = 0; f.BaseStream.Position < f.BaseStream.Length - 1; i++)
			{
				strings.Add(new Tuple<int, string>(i, ExtractString(f)));
			}

			return strings;
		}

		private static bool CheckSignature(BinaryReader br)
		{
			byte x = br.ReadByte();
			byte d = br.ReadByte();
			byte f = br.ReadByte();
			byte s = br.ReadByte();

			return x == 0x58 && d == 0x44 && f == 0x46 && s == 0x20;
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
					case 0x25:
						sb.Append("#");
						break;

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
