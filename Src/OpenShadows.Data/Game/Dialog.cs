﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenShadows.Data.Game
{
    public enum DialogStringEntryType
    {
        Party,
        Other,
        Emote
    }

    public class DialogStringEntry
    {
        public DialogStringEntryType Speaker = DialogStringEntryType.Party;

        public string String = string.Empty;

        public bool ShouldEndDialog = false;

        public override string ToString()
        {
            return $"{{ {Speaker}: {String} (Deadpoint? {ShouldEndDialog}) }}";
        }
    }

    public class DialogEntryPage
    {
        public List<DialogStringEntry> Strings = new List<DialogStringEntry>();

        public override string ToString()
        {
            StringBuilder sb = new();
            for (int i = 0; i < Strings.Count; i++)
            {
                var str = Strings[i];
                if (i > 0)
                {
                    sb.Append(" || ");
                }
                sb.Append(str);
            }
            sb.AppendLine();
            return sb.ToString();
        }
    }

    public class DialogEntry
    {
        public List<DialogEntryPage> Pages = new List<DialogEntryPage>();

        public override string ToString()
        {
            StringBuilder sb = new();
            for (int i = 0; i < Pages.Count; i++)
            {
                var str = Pages[i];
                if (i > 0)
                {
                    sb.Append(" || ");
                }
                sb.Append(str.ToString());
            }
            sb.AppendLine();
            return sb.ToString();
        }
    }

    public class DialogTopic
    {
        // Index into TOPIC.lXT (index, not offset into string table)
        public int TopicIndex { get; set; }

        // Entries for this topic
        // Show one after another per inquiry. Then switch to DEADPOIN.LXT for this topic.
        public List<DialogEntry> Entries { get; set; } = new List<DialogEntry>();
    }

    public class Dialog
    {
        public string CharName { get; set; } = string.Empty;

        public List<DialogTopic> Topics { get; set; } = new List<DialogTopic>();
    }
}
