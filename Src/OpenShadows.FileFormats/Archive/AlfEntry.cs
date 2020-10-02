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

		public Stream Open()
		{
			var bigStream = new AlfEntryStream(this, Archive);
			return new BufferedStream(bigStream);
		}
	}
}
