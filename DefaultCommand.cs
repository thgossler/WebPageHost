#nullable disable warnings

using Microsoft.Win32;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows.Forms;
using WebPageHost.Properties;
using WindowsInput;
using WindowsInput.Native;
using static WebPageHost.ScreenExtensions;

namespace WebPageHost
{

    internal sealed partial class OpenCommand : Command<OpenCommand.Settings>
    {
        [SuppressMessage("AsyncUsage", "AsyncFixer03:Fire-and-forget async-void methods or delegates", Justification = "<Pending>")]
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            AnsiConsole.MarkupLine($"Opening URL [green]{settings.Url}[/]...");

            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Hide the console window now after the argument processing is done
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);

            // Monitor placement
            var monitor = settings.MonitorNumber == -1 ? Screen.PrimaryScreen : Screen.AllScreens[settings.MonitorNumber];
            Point dpi = ScreenExtensions.GetMonitorDpi(monitor, DpiType.Effective);
            var scaleFactor = (float)dpi.X / 96;

            // Define WebView2 user folder name
            var userDataFolderName = Environment.UserName + ".WebView2";

            var form = new MainForm(userDataFolderName, settings.Url, settings.WindowTitle);
            form.FormLoaded += (s, e) =>
            {
                // Apply the specified zoom factor argument
                var form = (MainForm)s;
                form.webView.ZoomFactor = (double)settings.ZoomFactor;
            };
            form.FormClosing += (s, e) =>
            {
                // Save current windows bounds for potential use on next start
                this.LastWindowBounds = form.Bounds;

                // Print the current browser URL to stdout for external processing
                var lastUrl = form.Url;
                AnsiConsole.MarkupLine($"LastUrl=[green]{lastUrl}[/]");
            };

            // Try to auto-login on the web page with the specified credentials
            if (!String.IsNullOrWhiteSpace(settings.LoginCredentials))
            {
                var loginCreds = settings.LoginCredentials.Trim();
                var separatorIndex = loginCreds.IndexOf(',');
                var username = loginCreds.Substring(0, separatorIndex);
                var password = loginCreds.Substring(separatorIndex+1);
                form.WebViewDOMContentLoaded += async (s, e) =>
                {
                    var result = await form.webView.CoreWebView2.ExecuteScriptAsync(
                        "document.querySelector(\"input[type~='password']\") != null && " +
                        "window.getComputedStyle(document.querySelector(\"input[type~='password']\")).visibility != 'hidden'");
                    var isLoginPage = Boolean.Parse(result.Replace("\"", ""));
                    if (isLoginPage)
                    {
                        var autoLoginJs = Resources.AutoLoginJs;
                        autoLoginJs = autoLoginJs.Replace("$(USERNAME)", username).Replace("$(PASSWORD)", password);
                        await form.webView.CoreWebView2.ExecuteScriptAsync(autoLoginJs);
                        // Simulate Enter key press outside of script because it would not be trusted otherwise
                        Thread.Sleep(1000);
                        new InputSimulator().Keyboard.KeyPress(VirtualKeyCode.RETURN);
                    }
                };
            }

            // Load the window bounds from the last start
            var lastBounds = LastWindowBounds;

            // Apply the specified window size argument
            if (settings.WindowSizeArgument.Equals("Last", StringComparison.InvariantCultureIgnoreCase) && !lastBounds.Size.Equals(Size.Empty))
            {
                form.Size = lastBounds.Size;
            }
            else
            {
                form.Size = Helpers.Scale(settings.WindowSize, scaleFactor);
            }

            // Apply the specified window position argument
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
                form.Location = lastBounds.Location;
            }
            else
            {
                form.Location = new Point { X = monitor.WorkingArea.Left + settings.WindowPosition.X, Y = monitor.WorkingArea.Top + settings.WindowPosition.Y };
            }

            // Apply the specified window state argument
            form.WindowState = settings.WindowState;

            // Apply the specified top-most argument
            form.TopMost = settings.TopMost;

            // Show the UI
            Application.Run(form);

            // Clean-up when the application is about to close
            if (!settings.KeepUserData)
            {
                // Delete current user data folder
                DeleteWebView2UserDataFolder(userDataFolderName, form);
            }

            return 0;
        }

        private static void DeleteWebView2UserDataFolder(string userDataFolderName, MainForm form)
        {
            var exeFilePath = AppContext.BaseDirectory;
            var exeDirPath = Path.GetDirectoryName(exeFilePath);
            var dirInfo = new DirectoryInfo(exeDirPath);
            var userDataFolder = (DirectoryInfo)dirInfo.GetDirectories(userDataFolderName).GetValue(0);
            if (null != userDataFolder)
            {
                AnsiConsole.Markup($"[grey]Cleaning-up user data...[/] ");

                form.webView.Dispose(); // Ensure external browser process is exited

                bool success = false;
                int attempts = 0;
                while (!success && attempts <= 5)
                {
                    attempts++;
                    try
                    {
                        userDataFolder.Delete(true);
                        if (!userDataFolder.Exists)
                        {
                            success = true;
                            break;
                        }
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (DirectoryNotFoundException) { }
                    catch (IOException) { }
                    catch (SecurityException) { }
                    Thread.Sleep(1000);
                }
                if (!success)
                {
                    Trace.TraceWarning(String.Format("ERROR: WebView2 user data folder '{0}' could not be deleted", userDataFolder.FullName));
                }
                else
                {
                    AnsiConsole.MarkupLine($"[grey]Done.[/] ");
                }
            }
        }

        private Rectangle lastWindowBounds;
        public Rectangle LastWindowBounds
        {
            get
            {
                var bounds = new Rectangle();
                try
                {
                    // Read values from registry
                    var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\WebPageHost");
                    if (null != key)
                    {
                        bounds.X = (int)key.GetValue("LastWindowPosX");
                        bounds.Y = (int)key.GetValue("LastWindowPosY");
                        bounds.Width = (int)key.GetValue("LastWindowWidth");
                        bounds.Height = (int)key.GetValue("LastWindowHeight");
                    }
                    key.Close();
                }
                catch (Exception)
                {
                    Trace.TraceWarning("Last window bounds could not be read from registry");
                }
                finally
                {
                    lastWindowBounds = bounds;
                }
                return lastWindowBounds;
            }
            set
            {
                lastWindowBounds = value;

                // Store values in registry
                var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\WebPageHost");
                key.SetValue("LastWindowPosX", lastWindowBounds.X);
                key.SetValue("LastWindowPosY", lastWindowBounds.Y);
                key.SetValue("LastWindowWidth", lastWindowBounds.Width);
                key.SetValue("LastWindowHeight", lastWindowBounds.Height);
                key.Close();
            }
        }

        // Interop
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
    }
}
