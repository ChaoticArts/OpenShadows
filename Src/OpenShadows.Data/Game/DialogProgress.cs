using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenShadows.Data.Game
{
    public class DialogProgress
    {
        private bool isNew = true;

        private Dictionary<int, int> progressPerTopic = new Dictionary<int, int>();

        public int this[int topicId]
        {
            get { return GetProgress(topicId); }
            set { SetProgress(topicId, value); }
        }

        public bool IsNew => isNew;

        public int GetProgress(int topicId)
        {
            if (progressPerTopic.ContainsKey(topicId) == false) 
            {
                return 0;
            }

            return progressPerTopic[topicId];
        }

        public void SetProgress(int topicId, int value) 
        {
            isNew = false;

            progressPerTopic[topicId] = value;
        }
    }
}
