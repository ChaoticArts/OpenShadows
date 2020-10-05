using System;
using System.IO;

namespace OpenShadows.Data.Graphic
{
	public class Palette
	{
		private readonly byte[] PaletteData;

		private readonly int NumberOfColors;

		public Palette(int numberOfColors)
		{
			PaletteData    = new byte[numberOfColors * 3];
			NumberOfColors = numberOfColors;
		}

		public Palette(byte[] rawData, int numberOfColors)
		{
			PaletteData    = rawData;
			NumberOfColors = numberOfColors;

			// palette must be cleaned
			for (int i = 0; i < PaletteData.Length; i++)
			{
				int cleaned = PaletteData[i] & 0x3f;
				PaletteData[i] = (byte)(cleaned * 4);
			}
		}

		public byte[] GetColor(int idx)
		{
			var color = new byte[4];

			color[0] = PaletteData[idx * 3 + 0];
			color[1] = PaletteData[idx * 3 + 1];
			color[2] = PaletteData[idx * 3 + 2];
			color[3] = 0xff;

			return color;
		}

		public static Palette LoadFromPal(BinaryReader br)
		{
			var p = new Palette(256);

			br.ReadBytes(0x14);
			uint offset = br.ReadUInt32();
			var palOffset = SwapEndianess(offset) + 40;

			br.BaseStream.Seek(palOffset, SeekOrigin.Begin);

			for (int i = 0; i < 256; i++)
			{
				br.ReadByte();
				p.PaletteData[i * 3 + 0] = br.ReadByte();
				p.PaletteData[i * 3 + 1] = br.ReadByte();
				p.PaletteData[i * 3 + 2] = br.ReadByte();
			}

			return p;
		}

		private static uint SwapEndianess(uint n)
		{
			byte[] bytes = BitConverter.GetBytes(n);
			Array.Reverse(bytes);
			return BitConverter.ToUInt32(bytes);
		}
	}
}
