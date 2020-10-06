using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using OpenShadows.Data.Graphic;

namespace OpenShadows.FileFormats.Images
{
	public static class AceExtractor
	{
		public static AnimationSet ExtractImage(byte[] data)
		{
			using var f = new BinaryReader(new MemoryStream(data));

			if (!CheckSignature(f))
			{
				throw new InvalidDataException("Not a valid ACE image");
			}

			var animationSet = new AnimationSet();

			int version = f.ReadInt16();
			if (version != 0x01)
			{
				throw new InvalidDataException($"Unknown version {version}");
			}

			int paletteOffset = (int)f.BaseStream.Length - 256 * 3;
			long currentPosition = f.BaseStream.Position;
			f.BaseStream.Seek(paletteOffset, SeekOrigin.Begin);
			var palette = new Palette(f.ReadBytes(256 * 3), 256);
			f.BaseStream.Seek(currentPosition, SeekOrigin.Begin);

			int numberOfAnimations = f.ReadByte();
			int playbackSpeed      = f.ReadByte();

			if (numberOfAnimations == 1)
			{
				var animation = new Animation
				{
					Width  = f.ReadUInt16(),
					Height = f.ReadUInt16()
				};

				int numberFrames = f.ReadByte();
				int playbackMode = f.ReadByte();

				for (int i = 0; i < numberFrames; i++)
				{
					int compressedSize = f.ReadInt32();
					int xOffset        = f.ReadInt16();
					int yOffset        = f.ReadInt16();
					int frameWidth     = f.ReadInt16();
					int frameHeight    = f.ReadInt16();

					int compressionType = f.ReadByte();
					int actionButton    = f.ReadByte();

					var img = new ImageData
					{
						Height    = frameHeight,
						Width     = frameWidth,
						PixelData = new byte[frameWidth * frameHeight * 4]
					};

					byte[] uncompressedPixels = compressionType switch
					{
						0x01 => DecompressRleMode1(f, compressedSize, frameWidth * frameHeight),
						0x32 => DecompressPp20(f, compressedSize, frameWidth * frameHeight),
						_    => throw new NotSupportedException($"compression type {compressionType} is not supported")
					};

					using var ms = new MemoryStream(img.PixelData);
					foreach (byte uncompressedPixel in uncompressedPixels)
					{
						ms.Write(palette.GetColor(uncompressedPixel));
					}

					animation.Images.Add(img);
				}

				animationSet.Animations.Add(animation);
			}
			else
			{
				var offsets = new List<uint>();
				var numberOfFrames = new List<uint>();

				// read the animations toc
				for (int i = 0; i < numberOfAnimations; i++)
				{
					offsets.Add(f.ReadUInt32());

					var animation = new Animation
					{
						Id       = f.ReadUInt16(),
						Width    = f.ReadUInt16(),
						Height   = f.ReadUInt16(),
						HotspotX = f.ReadInt16(),
						HotspotY = f.ReadInt16()
					};

					numberOfFrames.Add(f.ReadByte());

					animation.Mode = f.ReadByte();

					animationSet.Animations.Add(animation);
				}

				for (int i = 0; i < numberOfAnimations; i++)
				{
					Animation animation = animationSet.Animations[i];
					uint      offset    = offsets[i];
					uint      frames    = numberOfFrames[i];

					for (int j = 0; j < frames; j++)
					{
						int compressedSize = f.ReadInt32();
						int xOffset        = f.ReadInt16();
						int yOffset        = f.ReadInt16();
						int frameWidth     = f.ReadInt16();
						int frameHeight    = f.ReadInt16();

						int compressionType = f.ReadByte();
						int actionButton    = f.ReadByte();

						var img = new ImageData
						{
							Height    = frameHeight,
							Width     = frameWidth,
							PixelData = new byte[frameWidth * frameHeight * 4]
						};

						byte[] uncompressedPixels = compressionType switch
						{
							0x01 => DecompressRleMode1(f, compressedSize, frameWidth * frameHeight),
							0x32 => DecompressPp20(f, compressedSize, frameWidth * frameHeight),
							_    => throw new NotSupportedException($"compression type {compressionType} is not supported")
						};

						using var ms = new MemoryStream(img.PixelData);
						foreach (byte uncompressedPixel in uncompressedPixels)
						{
							ms.Write(palette.GetColor(uncompressedPixel));
						}

						animation.Images.Add(img);
					}
				}
			}

			return animationSet;
		}

		private static bool CheckSignature(BinaryReader br)
		{
			byte a = br.ReadByte();
			byte c = br.ReadByte();
			byte e = br.ReadByte();
			byte x = br.ReadByte();

			return a == 0x41 && c == 0x43 && e == 0x45 && x == 0x00;
		}

		private static byte[] DecompressRleMode1(BinaryReader f, int compressedSize, int uncompressedSize)
		{
			byte[] compressedData   = f.ReadBytes(compressedSize);
			byte[] uncompressedData = new byte[uncompressedSize];

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

			return uncompressedData;
		}

		private static byte[] DecompressPp20(BinaryReader f, int compressedSize, int uncompressedSize)
		{
			// skip the first 4 bytes since they are just a repetition of the compressed size
			f.ReadBytes(4);

			byte[] compressedData   = f.ReadBytes(compressedSize - 4);
			byte[] uncompressedData = new byte[uncompressedSize];

			Native_UnpackPP20(compressedData, compressedData.Length, uncompressedData, uncompressedData.Length);

			return uncompressedData;
		}

		[DllImport("OpenShadows.Native.dll", EntryPoint = "unpack_pp20", CallingConvention = CallingConvention.StdCall)]
		private static extern int Native_UnpackPP20([In][Out] byte[] inData, int inSize, [In][Out] byte[] outData, int outSize);
	}
}
