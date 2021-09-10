#nullable disable warnings

using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WebPageHost
{
    internal sealed partial class OpenCommand
    {
        public sealed class Settings : CommandSettings
        {
            [Description("URL to open (only http/https supported).")]
            [CommandArgument(0, "<url>")]
            public string Url { get; init; }

            [Description("Text for the window title. Default: <url>")]
            [CommandOption("-t|--title")]
            public string? WindowTitle { get; init; }

            [Description("Window size (e.g. 1024x768), supported named values: \"Last\". Default: \"1024x768\"")]
            [CommandOption("-s|--size")]
            [DefaultValue("1024x768")]
            public string WindowSizeArgument { get; init; }

            public Size WindowSize
            {
                get {
                    Size size = Size.Empty;
                    string[] parts = WindowSizeArgument.Split('x');
                    size.Width = int.Parse(parts[0]);
                    size.Height = int.Parse(parts[1]);
                    return size;
                }
            }


            [Description("Window position (e.g. 100,80), supported named values: \"Last\", \"Center\". Default: \"Center\"")]
            [CommandOption("-p|--position")]
            [DefaultValue("Center")]
            public string WindowPositionArgument { get; init; }

            public Point WindowPosition
            {
                get
                {
                    if (WindowStateArgument.Equals("Last", StringComparison.InvariantCultureIgnoreCase) ||
                        WindowStateArgument.Equals("Center", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return Point.Empty;
                    }
                    Point pos = Point.Empty;
                    string[] parts = WindowPositionArgument.Split(',');
                    pos.X = int.Parse(parts[0]);
                    pos.Y = int.Parse(parts[1]);
                    return pos;
                }
            }

            [Description("Window state, supported values: \"Normal\", \"Minimized\", \"Maximized\". Default: \"Normal\"")]
            [CommandOption("-w|--windowstate")]
            [DefaultValue("Normal")]
            public string WindowStateArgument { get; init; }

            public FormWindowState WindowState
            {
                get {
                    var state = FormWindowState.Normal;
                    if (WindowStateArgument.Trim().Equals("Minimized", StringComparison.InvariantCultureIgnoreCase))
                    {
                        state = FormWindowState.Minimized;
                    }
                    else if (WindowStateArgument.Trim().Equals("Maximized", StringComparison.InvariantCultureIgnoreCase))
                    {
                        state = FormWindowState.Maximized;
                    }
                    return state;
                }
            }

            [Description("Target monitor number (e.g. 0 for first monitor). Default: primary monitor")]
            [CommandOption("-m|--monitor")]
            [DefaultValue("-1")]
            public int MonitorNumber { get; init; }

            [Description("The zoom factor for the web content. Default: 1.0")]
            [CommandOption("-z|--zoomfactor")]
            [DefaultValue((float)1.0)]
            public float ZoomFactor { get; init; }

            [Description("Keep window always on top of all other windows (top-most).")]
            [CommandOption("-o|--ontop")]
            [DefaultValue(false)]
            public bool TopMost { get; init; }

            [Description("Keep the WebView2 user data folder. Default: delete on exit")]
            [CommandOption("-k|--keepuserdata")]
            [DefaultValue(false)]
            public bool KeepUserData { get; init; }

            [Description("Credentials for auto-login on the web page, format: \"<username>,<password>\".")]
            [CommandOption("-l|--login")]
            public string? LoginCredentials { get; init; }

            public override ValidationResult Validate()
            {
                if (!(Url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) ||     //DevSkim: ignore DS137138
                      Url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase)) ||
                    Url.Length < 11)
                {
                    return ValidationResult.Error("Invalid URL specified (must start with \"http://\" or \"https://\" and be long enough)");  //DevSkim: ignore DS137138
                }

                if (!Regex.Match(WindowSizeArgument, @"\d+x\d+", RegexOptions.IgnoreCase).Success &&
                    !WindowPositionArgument.Equals("Last", StringComparison.InvariantCultureIgnoreCase))
                {
                    return ValidationResult.Error("Invalid windows size specified (allowed values: \"<w>,<h>\")");
                }

                if (!Regex.Match(WindowPositionArgument, @"\d+,\d+", RegexOptions.IgnoreCase).Success &&
                    !Regex.Match(WindowPositionArgument, "^(Last|Center)$", RegexOptions.IgnoreCase).Success)
                {
                    return ValidationResult.Error("Invalid window position specified (allowed values: \"<x>,<y>\" | \"Last\" | \"Center\")");
                }

                if (!Regex.Match(WindowStateArgument, @"^(Normal|Minimized|Maximized)$", RegexOptions.IgnoreCase).Success)
                {
                    return ValidationResult.Error("Invalid window state specified (allowed values: \"Normal\" | \"Minimized\" | \"Maximized\")");
                }

                if (MonitorNumber < -1 || MonitorNumber >= Screen.AllScreens.Length)
                {
                    var numOfMonitors = Screen.AllScreens.Length;
                    return ValidationResult.Error(String.Format("Invalid monitor number specified (number of monitors: {0}, value must be in range 0 .. {1})", numOfMonitors, numOfMonitors-1));
                }

                if (ZoomFactor < 0.1 || ZoomFactor > 3.0)
                {
                    return ValidationResult.Error("Invalid zoom factor, value must be in range 0.1 .. 3})");
                }

                if (null != LoginCredentials && !LoginCredentials.Contains(","))
                {
                    return ValidationResult.Error("Invalid login credentials specified");
                }

                return ValidationResult.Success();
            }
        }
    }
}
