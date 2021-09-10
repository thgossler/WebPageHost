#nullable disable warnings

using Microsoft.Win32;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using WebPageHost.Properties;
using WindowsInput;
using WindowsInput.Native;

namespace WebPageHost
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            var app = new CommandApp<OpenCommand>();
            app.Configure(config =>
            {
                config.SetApplicationName("WebPageHost");
                config.ValidateExamples();
            });
            return app.Run(args);
        }
    }
}
