using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using OpenShadows.Data.Graphic;

namespace OpenShadows.FileFormats.Images
{
	public static class OffExtractor
	{
		public static List<OffsetData> ExtractOffsets(byte[] data)
		{
			if (data.Length % 26 != 0)
			{
				throw new InvalidDataException("Not a valid OFF file");
			}

			using var f = new BinaryReader(new MemoryStream(data));

			var result = new List<OffsetData>();
			int offsets = data.Length / 26;
			for (int i = 0; i < offsets; i++)
			{
				var od = new OffsetData();
				od.Data = new ushort[13];
				for (int j = 0; j < 13; j++)
				{
					od.Data[j] = f.ReadUInt16();
				}
				result.Add(od);
			}
			return result;
		}
	}
}
