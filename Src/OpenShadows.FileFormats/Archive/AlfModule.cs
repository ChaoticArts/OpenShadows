using System.Collections.Generic;
using System.Linq;

namespace OpenShadows.FileFormats.Archive
{
	public class AlfModule
	{
		public string Name { get; set; }

		public int NumberOfFiles { get; set; }

		public int FirstIndexMappingTable { get; set; }

		internal List<AlfEntry> AlfEntries { get; set; } = new List<AlfEntry>();

		public List<AlfEntry> Entries => AlfEntries.OrderBy(e => e.Name).ToList();
	}
}
