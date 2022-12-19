using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenShadows.Data.Game
{
    public class StringTable
    {
        private string[] strings = Array.Empty<string>();

        public int Count => strings != null ? strings.Length : 0;

        public string this[int index]
        {
            get
            {
                if (index >= 0 && index < strings.Length) 
                {
                    return strings[index];
                }
                return string.Empty;
            }
        }

        public StringTable(string[] setStrings) 
        { 
            if (setStrings != null)
            {
                strings = setStrings;
            }
        }
    }
}
