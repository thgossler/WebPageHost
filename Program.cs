// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

#nullable disable warnings

using System;
using Spectre.Console.Cli;

namespace WebPageHost;

/// <summary>
/// Command line interface tool for opening web pages in a WebView2 control.
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var app = new CommandApp();
        app.Configure(config => {
            _ = config.SetApplicationName(Common.ProgramName);

            _ = config.AddCommand<OpenCommand>("open")
                .WithDescription("Opens the URL in a new window with an embedded web browser.")
                .WithExample(new[] { "--help" })
                .WithExample(new[] { "open", "--help" })
                .WithExample(new[] { "open", "https://www.google.com/", "--zoomfactor", "0.6" })
                .WithExample(new[] { "open", "https://www.google.com/", "-x", "document.title" });

            _ = config.AddCommand<CleanupCommand>("cleanup")
                .WithDescription("Resets the current user's web browser persistent data folder and registry settings.")
                .WithExample(new[] { "cleanup" });

            _ = config.ValidateExamples();
        });

        return app.Run(args);
    }
}
