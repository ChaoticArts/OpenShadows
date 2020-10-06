using System;
using System.Collections.Generic;
using System.Text;

namespace OpenShadows.Data.Graphic
{
	public class Animation
	{
		public List<ImageData> Images { get; set; } = new List<ImageData>();

		public uint Width { get; set; }

		public uint Height { get; set; }

		public int HotspotX { get; set; }

		public int HotspotY { get; set; }

		public uint Id { get; set; }

		public uint Mode { get; set; }
	}
}
