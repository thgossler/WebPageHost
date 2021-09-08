using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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

            [Description("Keep window always on top of all other windows (top-most).")]
            [CommandOption("-o|--ontop")]
            [DefaultValue(false)]
            public bool TopMost { get; init; }

            // Settings-level validation
            public override ValidationResult Validate()
            {
                if (!(Url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) ||     //DevSkim: ignore DS137138
                      Url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase)) ||
                    Url.Length < 11)
                {
                    return ValidationResult.Error("Invalid URL specified (must start with \"http://\" or \"https://\" and be long enough)");  //DevSkim: ignore DS137138
                }

                if (!Regex.Match(WindowSizeArgument, @"\d+x\d+", RegexOptions.IgnoreCase).Success)
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
                    return ValidationResult.Error(String.Format("Invalid monitor number specified (number of monitors: {0}, value must be in range 0..{1})", numOfMonitors, numOfMonitors-1));
                }

                return ValidationResult.Success();
            }
        }

        // Command-level validation
        public override ValidationResult Validate(CommandContext context, Settings settings)
        {
            return base.Validate(context, settings);
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.MarkupLine($"Opening URL [green]{settings.Url}[/]...");

            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);

            var form = new MainForm(settings.Url, settings.WindowTitle);
            form.FormClosing += Form_FormClosing;

            form.AutoScaleMode = AutoScaleMode.Dpi;

            // Monitor placement
            var monitor = settings.MonitorNumber == -1 ? Screen.PrimaryScreen : Screen.AllScreens[settings.MonitorNumber];
            Point dpi = ScreenExtensions.GetDpi(monitor, DpiType.Effective);
            var scaleFactor = (double)dpi.X / 96;
            T Scale<T>(T value)
            {
                T result = default(T);
                if (value is int)
                {
                    int val = (int)(object)value;
                    val = (int)(val * scaleFactor);
                    result = (T)(object)val;
                }
                else if (typeof(T) == typeof(Point))
                {
                    Point val = (Point)(object)value;
                    val.X = (int)(val.X * scaleFactor);
                    val.Y = (int)(val.Y * scaleFactor);
                    result = (T)(object)val;
                }
                else if (typeof(T) == typeof(Size))
                {
                    Size val = (Size)(object)value;
                    val.Width = (int)(val.Width * scaleFactor);
                    val.Height = (int)(val.Height * scaleFactor);
                    result = (T)(object)val;
                }
                else if (typeof(T) == typeof(Rectangle))
                {
                    Rectangle val = (Rectangle)(object)value;
                    val.X = (int)(val.X * scaleFactor);
                    val.Y = (int)(val.Y * scaleFactor);
                    val.Width = (int)(val.Width * scaleFactor);
                    val.Height = (int)(val.Height * scaleFactor);
                    result = (T)(object)val;
                }
                return result;
            }

            // Window size
            if (settings.WindowSizeArgument.Equals("Last", StringComparison.InvariantCultureIgnoreCase))
            {
                // TODO: handle "Last" correctly (save/load size)
                form.Size = new Size { Width = Scale(1024), Height = Scale(768) };
            }
            else
            {
                form.Size = Scale(settings.WindowSize);
            }

            // Window position
            form.StartPosition = FormStartPosition.Manual;
            if (settings.WindowPositionArgument.Equals("Center", StringComparison.InvariantCultureIgnoreCase))
            {
                form.Bounds = new Rectangle { 
                    X = monitor.WorkingArea.Left + (monitor.WorkingArea.Width - form.Width) / 2, 
                    Y = monitor.WorkingArea.Top + (monitor.WorkingArea.Height - form.Height) / 2, 
                    Width = form.Width,
                    Height = form.Height
                };
            }
            else if (settings.WindowPositionArgument.Equals("Last", StringComparison.InvariantCultureIgnoreCase))
            {
                // TODO: handle "Last" correctly (save/load position)
                form.Location = new Point { X = 100, Y = 100 };
            }
            else
            {
                form.Location = new Point { X = monitor.WorkingArea.Left + settings.WindowPosition.X, Y = monitor.WorkingArea.Top + settings.WindowPosition.Y };
            }

            // Window state
            form.WindowState = settings.WindowState;

            // Top-most
            form.TopMost = settings.TopMost;

            Application.Run(form);

            return 0;
        }

        private void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            var lastUrl = (sender as MainForm).Url;
            AnsiConsole.MarkupLine($"LastUrl=[green]{lastUrl}[/]");
        }
    }

    public static class ScreenExtensions
    {
        public static Point GetDpi(this Screen screen, DpiType dpiType)
        {
            var pnt = new Point(screen.Bounds.Left + 1, screen.Bounds.Top + 1);
            var mon = MonitorFromPoint(pnt, 2/*MONITOR_DEFAULTTONEAREST*/);
            uint dpiX;
            uint dpiY;
            GetDpiForMonitor(mon, dpiType, out dpiX, out dpiY);
            return new Point { X = (int)dpiX, Y = (int)dpiY };
        }

        //https://msdn.microsoft.com/en-us/library/windows/desktop/dd145062(v=vs.85).aspx
        [DllImport("User32.dll")]
        private static extern IntPtr MonitorFromPoint([In] Point pt, [In] uint dwFlags);

        //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510(v=vs.85).aspx
        [DllImport("Shcore.dll")]
        private static extern IntPtr GetDpiForMonitor([In] IntPtr hmonitor, [In] DpiType dpiType, [Out] out uint dpiX, [Out] out uint dpiY);
    }

    //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280511(v=vs.85).aspx
    public enum DpiType
    {
        Effective = 0,
        Angular = 1,
        Raw = 2,
    }
}
