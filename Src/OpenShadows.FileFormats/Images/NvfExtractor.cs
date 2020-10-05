using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using OpenShadows.Data.Graphic;

namespace OpenShadows.FileFormats.Images
{
	public static class NvfExtractor
	{
		public static List<ImageData> ExtractImage(byte[] data, List<Palette> extraPalettes = null)
		{
			using var f = new BinaryReader(new MemoryStream(data));

			if (!CheckSignature(f))
			{
				throw new InvalidDataException("Not a valid NVF image");
			}

			int nvfType        = f.ReadByte();
			int numberOfImages = f.ReadInt16();

			List<ImageData> imgs = nvfType switch
			{
				0x00 => ExtractType0(f, numberOfImages),
				0x01 => ExtractType1(f, numberOfImages),
				0x02 => ExtractType2_4(f, numberOfImages, 2),
				0x03 => ExtractType3_5(f, numberOfImages, 3),
				0x04 => ExtractType2_4(f, numberOfImages, 4, extraPalettes),
				0x05 => ExtractType3_5(f, numberOfImages, 5),
				_ => null
			};

			return imgs;
		}

		private static bool CheckSignature(BinaryReader br)
		{
			byte type = (byte) br.PeekChar();

			return type == 0x00 || type == 0x01 || type == 0x02 || type == 0x03 || type == 0x04 || type == 0x05;
		}

		private static List<ImageData> ExtractType0(BinaryReader f, int numberOfImages)
		{
			var imgs = new List<ImageData>();

			short width  = f.ReadInt16();
			short height = f.ReadInt16();

			int paletteOffset = width * height * numberOfImages + 0x07;
			f.BaseStream.Seek(paletteOffset, SeekOrigin.Begin);

			int numberOfColors = f.ReadInt16();
			var palette        = new Palette(f.ReadBytes(numberOfColors * 3), numberOfColors);

			for (int i = 0; i < numberOfImages; i++)
			{
				var img = new ImageData
				{
					Width     = width,
					Height    = height,
					PixelData = new byte[width * height * 4]
				};

				// seek to image
				f.BaseStream.Seek(0x07 + width * height * i, SeekOrigin.Begin);

				using var ms = new MemoryStream(img.PixelData);

				for (int j = 0; j < width * height; j++)
				{
					byte[] color = palette.GetColor(f.ReadByte());
					ms.Write(color, 0, 4);
				}

				imgs.Add(img);
			}

			return imgs;
		}

		private static List<ImageData> ExtractType1(BinaryReader f, int numberOfImages)
		{
			var imgs = new List<ImageData>();

			var resolutions = new List<Tuple<int, int>>();

			f.BaseStream.Seek(0x03, SeekOrigin.Begin);
			int pixelBlockSize = 0;
			for (int i = 0; i < numberOfImages; i++)
			{
				int width = f.ReadInt16();
				int height = f.ReadInt16();
				resolutions.Add(new Tuple<int, int>(width, height));
				pixelBlockSize += width * height;
			}

			int paletteOffset = 0x03 + numberOfImages * 4 + pixelBlockSize;
			f.BaseStream.Seek(paletteOffset, SeekOrigin.Begin);

			int numberOfColors = f.ReadInt16();
			var palette        = new Palette(f.ReadBytes(numberOfColors * 3), numberOfColors);

			int pixelOffset = 0x03 + numberOfImages * 4;
			for (int i = 0; i < numberOfImages; i++)
			{
				var img = new ImageData
				{
					Width     = resolutions[i].Item1,
					Height    = resolutions[i].Item2,
					PixelData = new byte[resolutions[i].Item1 * resolutions[i].Item2 * 4]
				};

				// seek to image
				f.BaseStream.Seek(pixelOffset, SeekOrigin.Begin);

				using var ms = new MemoryStream(img.PixelData);

				for (int j = 0; j < img.Width * img.Height; j++)
				{
					byte[] color = palette.GetColor(f.ReadByte());
					ms.Write(color, 0, 4);
					pixelOffset++;
				}

				imgs.Add(img);
			}

			return imgs;
		}

		private static List<ImageData> ExtractType2_4(BinaryReader f, int numberOfImages, int type, List<Palette> extraPalettes = null)
		{
			var imgs = new List<ImageData>();

			short width  = f.ReadInt16();
			short height = f.ReadInt16();

			var compressedSizes = new List<int>();

			f.BaseStream.Seek(0x07, SeekOrigin.Begin);
			int pixelBlockOffset = 0x07 + numberOfImages * 4;
			int pixelBlockSize   = 0;
			for (int i = 0; i < numberOfImages; i++)
			{
				int compressedSize = (int) f.ReadUInt32();
				compressedSizes.Add(compressedSize);
				pixelBlockSize   += compressedSize;
			}

			int paletteOffset = pixelBlockOffset + pixelBlockSize;
			f.BaseStream.Seek(paletteOffset, SeekOrigin.Begin);

			int numberOfColors = f.ReadInt16();
			var palette        = extraPalettes == null ? new Palette(f.ReadBytes(numberOfColors * 3), numberOfColors) : null;

			int offsetCompressedData = 0;
			for (int i = 0; i < numberOfImages; i++)
			{
				var img = new ImageData
				{
					Width     = width,
					Height    = height,
					PixelData = new byte[width * height * 4]
				};

				int compressedSize = compressedSizes[i];

				// seek to image
				f.BaseStream.Seek(pixelBlockOffset + offsetCompressedData + (type == 0x02 ? 4 : 0), SeekOrigin.Begin);

				byte[] uncompressedData = new byte[width * height];
				byte[] compressedData = f.ReadBytes(compressedSize - (type == 0x02 ? 4 : 0));

				// unpack
				if (type == 0x02)
				{
					Native_UnpackPP20(compressedData, compressedData.Length, uncompressedData, uncompressedData.Length);
				}
				else if (type == 0x04)
				{
					using var uncompressedStream = new MemoryStream(uncompressedData);
					int       offset             = 0;

					while (offset < compressedSize)
					{
						byte rle_data = compressedData[offset];
						offset++;

						if (rle_data == 0x7f)
						{
							int count = compressedData[offset];
							offset++;
							byte content = compressedData[offset];
							offset++;

							for (int j = 0; j < count; j++)
							{
								uncompressedStream.WriteByte(content);
							}
						}
						else
						{
							uncompressedStream.WriteByte(rle_data);
						}
					}
				}

				using var ms = new MemoryStream(img.PixelData);

				// special handling of DUALPICS.NVF
				if (extraPalettes != null)
				{
					palette = extraPalettes[i];
				}

				foreach (byte b in uncompressedData)
				{
					ms.Write(palette.GetColor(b));
				}

				offsetCompressedData += compressedSize;

				imgs.Add(img);
			}

			return imgs;
		}

		private static List<ImageData> ExtractType3_5(BinaryReader f, int numberOfImages, int type)
		{
			var imgs = new List<ImageData>();

			var resolutions     = new List<Tuple<int, int>>();
			var compressedSizes = new List<int>();

			f.BaseStream.Seek(0x03, SeekOrigin.Begin);
			int pixelBlockOffset = 0x03 + numberOfImages * 8;
			int pixelBlockSize   = 0;
			for (int i = 0; i < numberOfImages; i++)
			{
				int width          = f.ReadInt16();
				int height         = f.ReadInt16();
				int compressedSize = f.ReadInt32();

				resolutions.Add(new Tuple<int, int>(width, height));
				compressedSizes.Add(compressedSize);
				pixelBlockSize += compressedSize;
			}

			int paletteOffset = pixelBlockOffset + pixelBlockSize;
			f.BaseStream.Seek(paletteOffset, SeekOrigin.Begin);

			int numberOfColors = f.ReadInt16();
			var palette        = new Palette(f.ReadBytes(numberOfColors * 3), numberOfColors);

			int offsetCompressedData = 0;
			for (int i = 0; i < numberOfImages; i++)
			{
				var img = new ImageData
				{
					Width     = resolutions[i].Item1,
					Height    = resolutions[i].Item2,
					PixelData = new byte[resolutions[i].Item1 * resolutions[i].Item2 * 4]
				};

				int compressedSize = compressedSizes[i];

				// seek to image
				f.BaseStream.Seek(pixelBlockOffset + offsetCompressedData + (type == 0x03 ? 4 : 0), SeekOrigin.Begin);

				byte[] uncompressedData = new byte[img.Width * img.Height];
				byte[] compressedData   = f.ReadBytes(compressedSize - (type == 0x03 ? 4 : 0));

				// unpack
				if (type == 0x03)
				{
					Native_UnpackPP20(compressedData, compressedData.Length, uncompressedData, uncompressedData.Length);
				}
				else if (type == 0x05)
				{
					using var uncompressedStream = new MemoryStream(uncompressedData);
					int offset = 0;

					while (offset < compressedSize)
					{
						byte rle_data = compressedData[offset];
						offset++;

						if (rle_data == 0x7f)
						{
							int count = compressedData[offset];
							offset++;
							byte content = compressedData[offset];
							offset++;

							for (int j = 0; j < count; j++)
							{
								uncompressedStream.WriteByte(content);
							}
						}
						else
						{
							uncompressedStream.WriteByte(rle_data);
						}
					}
				}

				using var ms = new MemoryStream(img.PixelData);
				foreach (byte b in uncompressedData)
				{
					ms.Write(palette.GetColor(b));
				}

				offsetCompressedData += compressedSize;

				imgs.Add(img);
			}

			return imgs;
		}

		[DllImport("OpenShadows.Native.dll", EntryPoint = "unpack_pp20", CallingConvention = CallingConvention.StdCall)]
		private static extern int Native_UnpackPP20([In][Out] byte[] inData, int inSize, [In][Out] byte[] outData, int outSize);
	}
}
