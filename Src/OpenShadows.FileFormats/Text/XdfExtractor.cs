using OpenShadows.Data.Game;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static GLSLang.SpirV.Instruction;

namespace OpenShadows.FileFormats.Text
{
    public static class XdfExtractor
    {
        /// <summary>
        /// The keywords are NOT part of the XDF, yet they must still be relevant.
        /// 
        /// They're either hardcoded or part of another file.
        /// </summary>
        public static DialogDefinition ExtractDialog(byte[] data)
        {
            using var f = new BinaryReader(new MemoryStream(data));

            if (!CheckSignature(f))
            {
                throw new InvalidDataException("Not a valid XDF file");
            }

            DialogDefinition dialog = new DialogDefinition();

            //f.ReadBytes(0x20);

            var unk_a = f.ReadInt16();
            var unk_b = f.ReadInt16();

            var off1 = f.ReadInt32();
            var topicsOffset = f.ReadInt32();
            var topicDialogOffset = f.ReadInt32();
            var dialogEntriesOffset = f.ReadInt32();

            var unk = f.ReadInt32(); // 0x00 0x00 0x00 0x00
            var dialogPagesOffset = f.ReadInt32();

            var unk2 = f.ReadInt32(); // 0x00 0x00 0x00 0x00
            int offsetOfStrings = f.ReadInt32();

            // What about JOKES.LXT and RUMORS.LXT??

            // Could be name or shop/NPC identifier?
            f.BaseStream.Seek(off1, SeekOrigin.Begin);
            // Name offset into string table?
            dialog.CharName = GetString(f, offsetOfStrings, f.ReadInt32());
            dialog.HeadIndex = f.ReadUInt16();     // NPC/Location Index? (Index into HEADS.NVF, but maybe also index into animated screen in background => MASHOP.BOX ?; )
            _ = f.ReadUInt16();

            f.BaseStream.Seek(topicsOffset, SeekOrigin.Begin);
            while (f.BaseStream.Position < topicDialogOffset)
            {
                DialogTopic topic = new DialogTopic();

                var topicIndex = f.ReadUInt16();
                if (topicIndex == 0)
                {
                    break;
                }

                topic.TopicIndex = topicIndex;
                var offset = f.ReadInt32();
                if (offset != -1)
                {
                    topic.Entries.AddRange(ReadTopicEntries(f, topicDialogOffset, offset, dialogEntriesOffset, dialogPagesOffset, offsetOfStrings));
                }
                dialog.Topics.Add(topic);
            }

            return dialog;
        }

        private static List<DialogEntry> ReadTopicEntries(BinaryReader f, int topicDialogOffset, int offset, 
            int dialogEntriesOffset, int dialogPagesOffset, int offsetOfStrings)
        {
            var curPos = f.BaseStream.Position;
            try
            {
                List<DialogEntry> result = new List<DialogEntry>();
                f.BaseStream.Seek(topicDialogOffset + offset, SeekOrigin.Begin);

                int dialogoffset = f.ReadInt32();
                while (dialogoffset != -1)
                {
                    result.Add(ReadTopicEntry(f, dialogEntriesOffset, dialogoffset, dialogPagesOffset, offsetOfStrings));

                    // Relative offsets of DialogEntries per topic (incremental)
                    // Then => See DEADPOIN.LXT
                    dialogoffset = f.ReadInt32();
                }

                return result;
            }
            finally
            {
                f.BaseStream.Seek(curPos, SeekOrigin.Begin);
            }
        }

        private static DialogEntry ReadTopicEntry(BinaryReader f, int dialogEntriesOffset, int dialogoffset, 
            int dialogPagesOffset, int offsetOfStrings)
        {
            var curPos = f.BaseStream.Position;
            try
            {
                f.BaseStream.Seek(dialogEntriesOffset + dialogoffset, SeekOrigin.Begin);

                DialogEntry entry = new DialogEntry();

                ushort s_control = f.ReadUInt16();      // 0x01 => entry; 0x03 => end
                ushort s_unk = f.ReadUInt16();
                while (s_control != 0x03)
                {
                    int s_offset = f.ReadInt32();

                    // Read pages per entry
                    DialogEntryPage page = ReadDialogEntryPage(f, dialogPagesOffset, s_offset, offsetOfStrings);
                    entry.Pages.Add(page);

                    s_control = f.ReadUInt16();
                    ushort pad2 = f.ReadUInt16();
                }
                int pad4 = f.ReadInt32();

                return entry;
            }
            finally
            {
                f.BaseStream.Seek(curPos, SeekOrigin.Begin);
            }
        }

        private static DialogEntryPage ReadDialogEntryPage(BinaryReader f, int dialogEntriesOffset, int offset, int offsetOfStrings)
        {
            long curPos = f.BaseStream.Position;

            try
            {
                f.BaseStream.Seek(dialogEntriesOffset + offset, SeekOrigin.Begin);

                DialogEntryPage entry = new DialogEntryPage();

                ushort s_speaker = f.ReadUInt16();     // 0x04 => end?
                while (s_speaker != 0x04)
                {
                    var stringEntry = new DialogStringEntry();
                    stringEntry.Speaker = s_speaker switch
                    {
                        0x01 => DialogStringEntryType.Party,
                        0x02 => DialogStringEntryType.Other,
                        0x03 => DialogStringEntryType.Emote,
                        _ => DialogStringEntryType.Other
                    };

                    var s_unk4_a = f.ReadUInt16();

                    var shouldEndDialog = f.ReadUInt16();
                    // 0x05 => end of dialog?
                    stringEntry.ShouldEndDialog = shouldEndDialog != 0x00;

                    int s_pad5 = f.ReadInt32();

                    var stringTableOffset = f.ReadInt32();
                    stringEntry.String = GetString(f, offsetOfStrings, stringTableOffset);
                    entry.Strings.Add(stringEntry);

                    s_speaker = f.ReadUInt16();
                }

                int e_pad2 = f.ReadInt32();
                int e_pad3 = f.ReadInt32();
                int e_pad4 = f.ReadInt32();

                return entry;
            }
            finally
            {
                f.BaseStream.Seek(curPos, SeekOrigin.Begin);
            }
        }

        private static string GetString(BinaryReader f, int offsetOfStrings, int relativeOffset)
        {
            long curPos = f.BaseStream.Position;

            try
            {
                f.BaseStream.Seek(offsetOfStrings + relativeOffset, SeekOrigin.Begin);
                return Utils.ExtractString(f);
            }
            finally
            {
                f.BaseStream.Seek(curPos, SeekOrigin.Begin);
            }
        }

        private static bool CheckSignature(BinaryReader br)
        {
            byte x = br.ReadByte();
            byte d = br.ReadByte();
            byte f = br.ReadByte();
            byte s = br.ReadByte();

            return x == 0x58 && d == 0x44 && f == 0x46 && s == 0x20;
        }
    }
}
