#nullable disable warnings

using Spectre.Console.Cli;
using System;

namespace WebPageHost
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            var app = new CommandApp();
            app.Configure(config =>
            {
                config.SetApplicationName("WebPageHost");

                config.AddCommand<OpenCommand>("open")
                    .WithDescription("Opens the URL in a new window with an embedded web browser.")
                    .WithExample(new[] { "open", "https://www.google.com/", "--zoomfactor", "0.6" })
                    .WithExample(new[] { "open", "https://www.google.com/", "-x", "document.title" });

                config.AddCommand<CleanupCommand>("cleanup")
                    .WithDescription("Resets the current user's web browser persistent data folder and registry settings.")
                    .WithExample(new[] { "cleanup" });

                config.ValidateExamples();
            });
            
            return app.Run(args);
        }
    }
}
