using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenShadows.FileFormats.ALF
{
	public class AlfModule
	{
		public string Name { get; set; }

		public int NumberOfFiles { get; set; }

		public int FirstIndexMappingTable { get; set; }

		internal List<AlfEntry> AlfEntries { get; set; } = new List<AlfEntry>();

		public IOrderedEnumerable<AlfEntry> Entries => AlfEntries.OrderBy(e => e.Name);
	}
}
