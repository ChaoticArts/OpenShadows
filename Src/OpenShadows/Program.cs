using CommandLine;
using CommandLine.Text;
using Serilog.Events;
using Serilog;

namespace OpenShadows
{
    public class StartupOptions
    {
        public int Width { get; set; } = 1280;
        public int Height { get; set; } = 720;

        public bool Fullscreen { get; set; } = false;

        public string LevelName { get; set; } = string.Empty;
    }

    public static class Program
    {
        public static StartupOptions StartupOptions = new StartupOptions();

        public class CommandLineOptions
        {
            [Option('l', "level", Required = true, HelpText = "Level to load (without any extensions), e.g. RIVA01 or ENV02")]
            public string LevelName { get; set; }
        }

        static int Main(string[] args)
        {
            Parser p = new Parser((s) =>
            {
                //
            });

            var res = p.ParseArguments<CommandLineOptions>(args);
            res.WithParsed<CommandLineOptions>(o =>
                {
                    StartupOptions.LevelName = o.LevelName;
                });
            res.WithNotParsed<CommandLineOptions>(o =>
                {
                    Console.WriteLine(HelpText.AutoBuild(res, e =>
                    {
                        e.AdditionalNewLineAfterOption = false;
                        return e;
                    }, _ => _));
                });

            if (res.Errors.Count() > 0)
            {
                return 1;
            }

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

            Log.Information("StartupOptions.LevelName: " + StartupOptions.LevelName);

            OpenShadows openShadows = new OpenShadows();
            if (openShadows.Init(StartupOptions) == false)
            {
                return 2;
            }
            openShadows.Run();

            return 0;
        }
    }
}