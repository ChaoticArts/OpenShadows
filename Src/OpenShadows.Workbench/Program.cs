using System;
using System.IO;
using OpenShadows.FileFormats.ALF;
using OpenShadows.Workbench.Screens;

namespace OpenShadows.Workbench
{
	internal static class Program
	{
		private static void Main(string[] args)
		{
			using var ms = new MainScreen();
			ms.Run();
		}
	}
}
