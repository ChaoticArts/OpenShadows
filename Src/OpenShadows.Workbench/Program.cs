using OpenShadows.Workbench.Screens;
using Serilog;
using Serilog.Events;

namespace OpenShadows.Workbench
{
	internal static class Program
	{
        internal static MainScreen MainScreen;

		private static void Main()
		{
            var config = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Verbose()
#else
                .MinimumLevel.Information()
#endif
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console();

            Log.Logger = config.CreateLogger();

            using (MainScreen = new MainScreen())
            {
                MainScreen.Run();
            }
        }
	}
}
