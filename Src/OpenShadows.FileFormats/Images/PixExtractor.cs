﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using OpenShadows.Data.Graphic;

namespace OpenShadows.FileFormats.Images
{
	public static class PixExtractor
	{
        public static ImageData ExtractImage(byte[] data, Palette palette)
		{
			using var f = new BinaryReader(new MemoryStream(data));

			// skip
			f.ReadBytes(0x0a);

			// unpack BoPa
			uint uncompressedSize = Utils.SwapEndianess(f.ReadUInt32());
			uint compressedSize   = Utils.SwapEndianess(f.ReadUInt32());
			var  uncompressedData = new byte[uncompressedSize];
			var  compressedData   = f.ReadBytes((int)compressedSize);

            Utils.UnpackBoPa(compressedData, compressedSize, uncompressedData, uncompressedSize);

			// read pix data
			var img = new ImageData
			{
				Width  = Utils.SwapEndianess(BitConverter.ToUInt16(uncompressedData, 0x1b)),
				Height = Utils.SwapEndianess(BitConverter.ToUInt16(uncompressedData, 0x1d))
			};

			img.PixelData = new byte[img.Width * img.Height * 4];

			uint uncompressedOffset = 40 + Utils.SwapEndianess(BitConverter.ToUInt32(uncompressedData, 0x14));

			for (int i = 0; i < img.Width * img.Height; i++)
			{
				var color = palette.GetColor(uncompressedData[i + uncompressedOffset]);
				img.PixelData[i * 4 + 0] = color[0];
				img.PixelData[i * 4 + 1] = color[1];
				img.PixelData[i * 4 + 2] = color[2];
				img.PixelData[i * 4 + 3] = color[3];
			}

			return img;
		}
	}
}
