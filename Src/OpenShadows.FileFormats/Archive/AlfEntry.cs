using System;
using System.IO;

namespace OpenShadows.FileFormats.Archive
{
	public class AlfEntry
	{
		public string Name { get; set; }

		public int Size { get; set; }

		public int Offset { get; set; }

		public int Index { get; set; }

		public AlfArchive Archive { get; set; }

		public byte[] GetContents()
		{
			return Archive.GetContents(this);
		}
	}
}
