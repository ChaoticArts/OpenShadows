using CommandLine;
using CommandLine.Text;
using Serilog.Events;
using Serilog;
using CommandLine.Infrastructure;

namespace OpenShadows
{
    public class StartupOptions
    {
        public int Width { get; set; } = 1280;
        public int Height { get; set; } = 720;

        public bool Fullscreen { get; set; } = false;

        public string GameFolder { get; set; } = string.Empty;
    }

    public static class Program
    {
        public static StartupOptions StartupOptions = new StartupOptions();

        public class CommandLineOptions
        {
            [Option('g', "game", Required = false, HelpText = "Location of Shadows over Riva installation (where RIVA.EXE is)")]
            public string GameFolder { get; set; } = string.Empty;
        }

        static int Main(string[] args)
        {
            Parser p = new Parser((s) =>
            {
                //
            });

            var cmdLineGameFolder = string.Empty;

            var res = p.ParseArguments<CommandLineOptions>(args);
            res.WithParsed<CommandLineOptions>(o =>
            {
                cmdLineGameFolder = o.GameFolder;
            });
            res.WithNotParsed<CommandLineOptions>(o =>
            {
                o = o
                .Where(e => !e.StopsProcessing)
                .Where(e => !(e.Tag == ErrorType.UnknownOptionError
                    && EqualsOrdinalIgnoreCase(((UnknownOptionError)e).Token, "help")));
                if (o.Count() > 0)
                {
                    Console.WriteLine(HelpText.AutoBuild(res, e =>
                    {
                        e.AdditionalNewLineAfterOption = false;
                        return e;
                    }, _ => _));
                }
            });

            if (res.Errors.Count() > 0)
            {
                return 1;
            }

            SetupLogger();

            if (cmdLineGameFolder != string.Empty)
            {
                StartupOptions.GameFolder = cmdLineGameFolder;
                Log.Information("StartupOptions.GameFolder: " + StartupOptions.GameFolder);
            }

            OpenShadows openShadows = new OpenShadows();
            if (openShadows.Init(StartupOptions) == false)
            {
                return 2;
            }
            openShadows.Run();

            return 0;
        }

        private static void SetupLogger()
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
        }

        private static bool EqualsOrdinalIgnoreCase(string strA, string strB)
        {
            return string.Compare(strA, strB, StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}