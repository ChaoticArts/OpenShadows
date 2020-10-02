using System;
using System.Collections.Generic;
using System.Text;

namespace OpenShadows.Workbench
{
	public static class StringExtensions
	{
		public static string Start(this string s, int count)
		{
			return count >= s.Length ? s : s.Substring(0, count);
		}

		public static string End(this string s, int count)
		{
			return count >= s.Length ? s : s.Substring(s.Length - count);
		}
	}
}
