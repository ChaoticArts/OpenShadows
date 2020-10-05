using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenShadows.Data.Graphic;

namespace OpenShadows.FileFormats.Images
{
	public static class AifExtractor
	{
		public static ImageData ExtractImage(byte[] data)
		{
			using var f = new BinaryReader(new MemoryStream(data));

			if (!CheckSignature(f))
			{
				throw new InvalidDataException("Not a valid AIF image");
			}

			var img = new ImageData();

			f.BaseStream.Seek(0x04, SeekOrigin.Begin);
			short type = f.ReadInt16();

			img.Width  = f.ReadInt16();
			img.Height = f.ReadInt16();
			img.PixelData = new byte[img.Width * img.Height * 4];

			switch (type)
			{
				case 2:
					ExtractType2(f, ref img);
					break;

				case 3:
					ExtractType3(f, ref img);
					break;

				default:
					throw new InvalidDataException($"Unknown AIF type {type:X}");
			}

			return img;
		}

		public static int GetType(Stream imgStream)
		{
			using var f = new BinaryReader(imgStream);

			if (!CheckSignature(f))
			{
				throw new InvalidDataException("Not a valid AIF image");
			}

			f.BaseStream.Seek(0x04, SeekOrigin.Begin);

			return f.ReadInt16();
		}

		private static bool CheckSignature(BinaryReader br)
		{
			byte a = br.ReadByte();
			byte i = br.ReadByte();
			byte f = br.ReadByte();

			return a == 0x41 && i == 0x49 && f == 0x46;
		}

		private static void ExtractType2(BinaryReader br, ref ImageData img)
		{
			using var ms = new MemoryStream(img.PixelData);

			int numberOfColors = br.ReadInt16();

			int sizeOfCompressedBlock = (int) br.BaseStream.Length - numberOfColors * 3 - 0x1e;

			br.BaseStream.Seek(sizeOfCompressedBlock + 0x1e, SeekOrigin.Begin);

			var p = new Palette(br.ReadBytes(numberOfColors * 3), numberOfColors);

			br.BaseStream.Seek(0x1e, SeekOrigin.Begin);

			int readBytes = 0;
			while (readBytes < sizeOfCompressedBlock)
			{
				byte control = br.ReadByte();
				readBytes++;

				if (control > 127)
				{
					int times = (byte)(1 - control);
					byte value = br.ReadByte();
					readBytes++;

					for (int i = 0; i < times; i++)
					{
						byte[] color = p.GetColor(value);
						ms.Write(color, 0, 4);
					}
				}
				else
				{
					int times = control + 1;

					for (int i = 0; i < times; i++)
					{
						byte value = br.ReadByte();
						readBytes++;

						byte[] color = p.GetColor(value);
						ms.Write(color, 0, 4);
					}
				}
			}
		}

		private static void ExtractType3(BinaryReader br, ref ImageData img)
		{
			using var ms = new MemoryStream(img.PixelData);

			int numberOfColors = br.ReadInt16();

			int sizeOfCompressedBlock = (int)br.BaseStream.Length - numberOfColors * 3 - 0x1e;

			br.BaseStream.Seek(sizeOfCompressedBlock + 0x1e, SeekOrigin.Begin);

			var p = new Palette(br.ReadBytes(numberOfColors * 3), numberOfColors);

			const int compressedBlockStart = 0x1e + 0x04;
			br.BaseStream.Seek(compressedBlockStart, SeekOrigin.Begin);

			byte[] compressedData   = br.ReadBytes(sizeOfCompressedBlock - 4);
			byte[] uncompressedData = new byte[img.Height * img.Width];

			if (Native_UnpackPP20(compressedData, compressedData.Length, uncompressedData, uncompressedData.Length) != 0)
			{
				throw new Exception("could not unpack PP20");
			}

			foreach (byte b in uncompressedData)
			{
				ms.Write(p.GetColor(b));
			}
		}

		[DllImport("OpenShadows.Native.dll", EntryPoint = "unpack_pp20", CallingConvention = CallingConvention.StdCall)]
		private static extern int Native_UnpackPP20([In][Out] byte[] inData, int inSize, [In][Out] byte[] outData, int outSize);
	}
}
