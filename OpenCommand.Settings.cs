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
            [Description("URL to open (only supports http/https protocols).")]
            [CommandArgument(0, "<url>")]
            public string Url { get; init; }

            [Description("Text for the window title. Default: <url>")]
            [CommandOption("-t|--title")]
            public string? WindowTitle { get; init; }

            [Description("Window size (e.g. 1024x768), supported named values: \"Last\". Default: \"1280x720\"")]
            [CommandOption("-s|--size")]
            [DefaultValue("1280x720")]
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


            [Description("Window location (e.g. 100,80), supported named values: \"Last\", \"Center\". Default: \"Center\"")]
            [CommandOption("-l|--location")]
            [DefaultValue("Center")]
            public string WindowLocationArgument { get; init; }

            public Point WindowLocation
            {
                get
                {
                    if (WindowStateArgument.Equals("Last", StringComparison.InvariantCultureIgnoreCase) ||
                        WindowStateArgument.Equals("Center", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return Point.Empty;
                    }
                    Point pos = Point.Empty;
                    string[] parts = WindowLocationArgument.Split(',');
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

            [Description("User name for auto-login on the web page.")]
            [CommandOption("-u|--user")]
            public string? Username { get; init; }

            [Description("Password for auto-login on the web page.")]
            [CommandOption("-p|--password")]
            public string? Password { get; init; }

            [Description("Time interval in seconds for automatic reload.")]
            [CommandOption("-r|--refresh")]
            [DefaultValue(0)]
            public int RefreshIntervalInSecs { get; init; }

            // TODO: Support option AllowSingleSignOnUsingOSPrimaryAccount

            // TODO: Add option to only output last URL / selected value in stdout for easier parsing by callers

            // TODO: Add extension mechanism: specify result content selection plugin by argument (without plugin by default the last URL is returned)


            public override ValidationResult Validate()
            {
                if (!(Url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) ||     //DevSkim: ignore DS137138
                      Url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase)) ||
                    Url.Length < 11)
                {
                    return ValidationResult.Error("Invalid URL specified (must start with \"http://\" or \"https://\" and be long enough)");  //DevSkim: ignore DS137138
                }

                if (!Regex.Match(WindowSizeArgument, @"\d+x\d+", RegexOptions.IgnoreCase).Success &&
                    !WindowLocationArgument.Equals("Last", StringComparison.InvariantCultureIgnoreCase))
                {
                    return ValidationResult.Error("Invalid windows size specified (allowed values: \"<w>,<h>\")");
                }

                if (!Regex.Match(WindowLocationArgument, @"\d+,\d+", RegexOptions.IgnoreCase).Success &&
                    !Regex.Match(WindowLocationArgument, "^(Last|Center)$", RegexOptions.IgnoreCase).Success)
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
                    return ValidationResult.Error("Invalid zoom factor, value must be in range 0.1 .. 3");
                }

                if (!String.IsNullOrEmpty(Password) && String.IsNullOrEmpty(Username))
                {
                    return ValidationResult.Error("Username was not specified");
                }

                if (RefreshIntervalInSecs < 0 || RefreshIntervalInSecs > 86400)
                {
                    return ValidationResult.Error("Invalid refresh interval (seconds), value must be in range 1 .. 86400 (24 hours)");
                }

                return ValidationResult.Success();
            }
        }
    }
}
