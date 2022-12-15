using OpenShadows.Workbench.Screens;
using Serilog;
using Serilog.Events;

namespace OpenShadows.Workbench
{
	internal static class Program
	{
		private static void Main()
		{
            var config = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console();

            Log.Logger = config.CreateLogger();

            using var ms = new MainScreen();
			ms.Run();
		}
	}
}
