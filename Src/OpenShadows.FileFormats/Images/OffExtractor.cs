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
		/// <summary>
		/// OFF files are always a multiple of 26 bytes in length and always have a matching
		/// .NVF file associated with it. The .NVF contains n images which directly correlate 
		/// to the entries in the matching OFF file. 
		/// Additionally there is a matching full screen battle image associated.
		/// Example:
		/// QU_PART.NVF has two images in it
		/// QU_PART.OFF has two 26 byte entries in it (52 bytes in total)
		/// QU_BACK.AIF is the whole battle screen.
		/// 
		/// The OFF entries somehow define the location of the object on the corresponding 
		/// battle screen.
		/// 
		/// Theory:
		/// The data in the OFF does not match to pixels, but it may match to the cells in
		/// the battle screen (unconfirmed).
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		/// <exception cref="InvalidDataException"></exception>
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
