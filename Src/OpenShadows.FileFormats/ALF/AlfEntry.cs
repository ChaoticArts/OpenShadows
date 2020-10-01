using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenShadows.FileFormats.ALF
{
	public class AlfEntry
	{
		public string Name { get; set; }

		public int Size { get; set; }

		public int Offset { get; set; }

		public int Index { get; set; }

		public AlfArchive Archive { get; set; }

		public Stream Open()
		{
			var bigStream = new AlfEntryStream(this, Archive);
			return new BufferedStream(bigStream);
		}
	}
}
