using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace OpenShadows.FileFormats.ALF
{
	public class AlfArchive : IDisposable
	{
		private readonly FileStream AlfStream;

		private readonly List<AlfEntry> AlfEntries = new List<AlfEntry>();

		private readonly List<AlfModule> AlfModules = new List<AlfModule>();

		internal Stream Stream => AlfStream;

		private object FileLock = new object();

		public long Size => AlfStream.Length;

		public IReadOnlyList<AlfEntry> Entries => AlfEntries;

		public IEnumerable<AlfModule> Modules => AlfModules.OrderBy(m => m.Name);

		public AlfArchive(string filename)
		{
			AlfStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);

			ReadToC();
		}

		public void Dispose()
		{
			AlfStream?.Dispose();
		}

		private void ReadToC()
		{
			using var file = new BinaryReader(AlfStream, Encoding.ASCII, true);

			// Read Identifier "ALF "
			if (file.ReadUInt32() != 0x20464c41)
			{
				throw new InvalidDataException("Die Datei ist kein gültiges ALF-Archiv");
			}

			// Read File Table
			file.BaseStream.Seek(0x0a, SeekOrigin.Begin);
			int fileTableOffset = file.ReadInt32();
			int numberOfFiles = file.ReadInt16();

			file.BaseStream.Seek(fileTableOffset, SeekOrigin.Begin);

			for (int i = 0; i < numberOfFiles; i++)
			{
				var entry = new AlfEntry
				{
					Index = i
				};

				char[] nameA = file.ReadChars(13);
				entry.Name = new string(nameA, 0, nameA.TakeWhile(c => c != '\0').Count());

				file.ReadChar();

				entry.Size = file.ReadInt32();

				file.ReadChars(6);

				entry.Offset = file.ReadInt32() + 0x30;
				entry.Archive = this;

				AlfEntries.Add(entry);
			}

			// Read Module Table
			file.BaseStream.Seek(0x16, SeekOrigin.Begin);
			int moduleTableOffset = file.ReadInt32();
			int numberOfModules = file.ReadInt16();

			file.BaseStream.Seek(moduleTableOffset, SeekOrigin.Begin);

			for (int i = 0; i < numberOfModules; i++)
			{
				var module = new AlfModule();

				char[] nameA = file.ReadChars(14);
				module.Name = new string(nameA, 0, nameA.TakeWhile(c => c != '\0').Count());

				module.NumberOfFiles = file.ReadInt32();

				file.ReadChars(6);

				module.FirstIndexMappingTable = file.ReadInt32();

				AlfModules.Add(module);
			}

			// Read Module File Mapping Table
			file.BaseStream.Seek(0x1c, SeekOrigin.Begin);
			int moduleMappingTableOffset = file.ReadInt32();

			file.BaseStream.Seek(moduleMappingTableOffset, SeekOrigin.Begin);

			foreach (AlfModule module in AlfModules)
			{
				for (int i = 0; i < module.NumberOfFiles; i++)
				{
					ushort idx = file.ReadUInt16();

					if (idx == 0xffff)
					{
						continue;
					}

					if (idx > AlfEntries.Count)
					{
						throw new InvalidDataException($"Verweis zeigt auf nicht vorhandene Datei ({file.BaseStream.Position})");
					}

					AlfEntry newEntry = AlfEntries[idx];

					if (!module.AlfEntries.Contains(newEntry))
					{
						module.AlfEntries.Add(newEntry);
					}
				}
			}
		}

		public void AcquireLock()
		{
			Monitor.Enter(FileLock);
		}

		public void ReleaseLock()
		{
			Monitor.Exit(FileLock);
		}
	}
}
