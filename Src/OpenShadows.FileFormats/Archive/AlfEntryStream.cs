using System;
using System.IO;

namespace OpenShadows.FileFormats.Archive
{
	public class AlfEntryStream : Stream
	{
		private readonly AlfEntry Entry;
		private readonly AlfArchive Archive;
		private readonly int Offset;
		private bool IsArchiveLocked;

		public AlfEntryStream(AlfEntry entry, AlfArchive archive)
		{
			Entry = entry;
			Archive = archive;
			Offset = entry.Offset;

			Archive.AcquireLock();
			IsArchiveLocked = true;
		}

		public override void Flush()
		{
			throw new NotSupportedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			Archive.Stream.Seek(Offset + Position, SeekOrigin.Begin);
			if (count > (Length - Position))
			{
				count = (int)(Length - Position);
			}

			int result = Archive.Stream.Read(buffer, offset, count);
			Position += result;

			return result;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			switch (origin)
			{
				case SeekOrigin.Begin:
					Position = offset;
					break;

				case SeekOrigin.Current:
					Position += offset;
					break;

				case SeekOrigin.End:
					Position = Length + offset;
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
			}

			return Position;
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		public override bool CanRead => Archive.Stream.CanRead;

		public override bool CanSeek => Archive.Stream.CanSeek;

		public override bool CanWrite => false;

		public override long Length => Entry.Size;

		public override long Position { get; set; }

		public override void Close()
		{
			if (IsArchiveLocked)
			{
				Archive.ReleaseLock();
				IsArchiveLocked = false;
			}

			base.Close();
		}
	}
}
