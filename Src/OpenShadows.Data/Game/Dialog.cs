using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace OpenShadows.Data.Game
{
    /// <summary>
    /// Manages a dialog at runtime.
    /// </summary>
    public class Dialog
    {
        private DialogDefinition dialogDefinition;
        private DialogProgress dialogProgress;
        private StringTable topicStringTable;
        private StringTable deadpointStringTable;
        private int[] knownTopics;

        public string Title => dialogDefinition.CharName;

        public bool HasEnded { get; private set; } = false;

        public Dialog(DialogDefinition setDialogDefinition, DialogProgress setDialogProgress, int[] setKnownTopics,
            StringTable setTopicStringTable, StringTable setDeadpointStringTable) 
        { 
            dialogDefinition = setDialogDefinition;
            dialogProgress = setDialogProgress;
            topicStringTable = setTopicStringTable;
            deadpointStringTable = setDeadpointStringTable;
            knownTopics = setKnownTopics;
        }

        public void Restart()
        {
            HasEnded = false;
        }

        /// <summary>
        /// Returns information about the partner
        /// </summary>
        public (string name, int id) GetPartnerInfo()
        {
            return (dialogDefinition.CharName, dialogDefinition.HeadIndex);
        }

        /// <summary>
        /// Returns the greeting text
        /// </summary>
        public DialogEntry? GetGreeting()
        {
            if (dialogProgress.IsNew)
            {
                return GetEntryForTopic(28, true);
            }
            else
            {
                return GetEntryForTopic(29, false);
            }
        }

        /// <summary>
        /// Get strings for the currently relevant topics
        /// </summary>
        public (string name, int topicIndex)[] GetTopicStrings(bool includeHidden = false)
        {
            List<(string name, int topicIndex)> result = new List<(string name, int topicIndex)>();
            for (int i = 0; i < dialogDefinition.Topics.Count; i++)
            {
                var topic = dialogDefinition.Topics[i];
                var topicString = topicStringTable[topic.TopicIndex];
                if (includeHidden == false && topicString.StartsWith("!"))
                {
                    continue;
                }
                if (knownTopics.Contains(topic.TopicIndex))
                {
                    result.Add((topicString, topic.TopicIndex));
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Returns the relevant DialogEntry for the given topic.
        /// If <paramref name="markAsProgressed"/> is true, the dialog progress will be updated
        /// to reflect the progress in the dialog. If there are no more steps for the respective
        /// topic, the deadpoint for the topic will be returned.
        /// </summary>
        public DialogEntry? GetEntryForTopic(int topicIndex, bool markAsProgressed)
        {
            var topic = dialogDefinition.FindTopicById(topicIndex);
            if (topic == null)
            {
                return null;
            }

            int currentStep = dialogProgress.GetProgress(topicIndex);
            if (currentStep < topic.Entries.Count) 
            { 
                var entry = topic.Entries[currentStep];
                if (markAsProgressed)
                {
                    dialogProgress.SetProgress(topicIndex, currentStep + 1);
                }
                if (entry.ShouldEndDialogAfterLastPage)
                {
                    HasEnded = true;
                }
                return entry;
            }

            return GetDeadpointEntry(topicIndex);
        }

        private DialogEntry GetDeadpointEntry(int topicIndex) 
        {
            topicIndex -= 1;

            var partyString = deadpointStringTable[topicIndex * 2 + 0];
            var otherString = deadpointStringTable[topicIndex * 2 + 1];
            var res = new DialogEntry();
            var page = new DialogEntryPage();
            page.Strings.Add(new DialogStringEntry()
            {
                Speaker = DialogStringEntryType.Party,
                String = partyString,
                ShouldEndDialog = false
            });
            page.Strings.Add(new DialogStringEntry()
            {
                Speaker = DialogStringEntryType.Other,
                String = otherString,
                ShouldEndDialog = false
            });
            res.Pages.Add(page);
            return res;
        }
    }
}
