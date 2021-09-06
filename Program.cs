using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WebPageHost
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            var app = new CommandApp<DefaultCommand>();
            app.Configure(config =>
            {
                config.SetApplicationName("WebPageHost");
                config.ValidateExamples();
            });
            return app.Run(args);
        }
    }

    internal sealed class DefaultCommand : Command<DefaultCommand.Settings>
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        public sealed class Settings : CommandSettings
        {
            [Description("URL to open.")]
            [CommandArgument(0, "<url>")]
            public string Url { get; init; }

            // TODO: specify window position/size
            // TODO: reuse last position
            // TODO: initial window state (min, max, normal)
            // TODO: specify monitor

            // Settings-level validation
            public override ValidationResult Validate()
            {
                return (Url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) || Url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                    && Url.Length >= 11
                    ? ValidationResult.Success()
                    : ValidationResult.Error("Invalid URL was specified");
            }
        }

        // Command-level validation
        public override ValidationResult Validate(CommandContext context, Settings settings)
        {
            return base.Validate(context, settings);
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            var url = settings.Url;

            AnsiConsole.MarkupLine($"Opening URL [green]{url}[/]...");

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);

            Application.Run(new MainForm(url));

            return 0;
        }
    }
}
