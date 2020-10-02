using OpenShadows.Workbench.Screens;

namespace OpenShadows.Workbench
{
	internal static class Program
	{
		private static void Main()
		{
			using var ms = new MainScreen();
			ms.Run();
		}
	}
}
