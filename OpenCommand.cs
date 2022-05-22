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
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using static WebPageHost.ScreenExtensions;

namespace WebPageHost
{
    internal sealed partial class OpenCommand : Command<OpenCommand.Settings>
    {
        /// <summary>
        /// Open command implementation.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="settings">The provided command line arguments.</param>
        /// <returns>Program exit code</returns>
        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            // If a script for custom output is specified then do not output other logs
            bool logToStdout = String.IsNullOrWhiteSpace(settings.ResultJavaScript);

            if (logToStdout) AnsiConsole.MarkupLine($"Opening URL [green]{settings.Url}[/]...");

            // Initialize WinForms
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            var consoleWindowHandle = GetConsoleWindow();
            if (settings.HideConsole)
            {
                // Hide the console window now after start and argument processing done
                ShowWindow(consoleWindowHandle, SW_HIDE);
            }

            // Determine monitor placement
            var monitor = settings.MonitorNumber == -1 ? Screen.PrimaryScreen : Screen.AllScreens[settings.MonitorNumber];
            Point dpi = ScreenExtensions.GetMonitorDpi(monitor, DpiType.Effective);
            var scaleFactor = (float)dpi.X / 96;

            // Get WebView2 user folder name
            var userDataFolderName = Common.WebView2UserDataFolderName;

            var form = new MainForm(userDataFolderName, settings.Url, settings.WindowTitle, settings.DisableSingleSignOnUsingOSPrimaryAccount);
            form.FormLoaded += (s, e) =>
            {
                // Apply the specified zoom factor
                var form = (MainForm)s;
                form.webView.ZoomFactor = (double)settings.ZoomFactor;
            };
            form.FormClosing += async (s, e) =>
            {
                // Get a custom exit result by executing the provided JavaScript statement on the web page
                if (!String.IsNullOrWhiteSpace(settings.ResultJavaScript) && null == javaScriptResult)
                {
                    javaScriptResult = string.Empty;
                    e.Cancel = true;
                    javaScriptResult = await form.webView.ExecuteScriptAsync(settings.ResultJavaScript);
                    AnsiConsole.WriteLine(javaScriptResult);
                    form.Close();
                }

                // Save current windows bounds for potential use on next start
                this.LastWindowBounds = form.Bounds;

                // Print the current browser URL to stdout for external processing
                var lastUrl = form.Url;
                if (logToStdout) AnsiConsole.MarkupLine($"LastUrl=[green]{lastUrl}[/]");
            };

            // Refresh the current web page after the specified interval
            System.Threading.Timer refreshTimer = null;
            if (settings.RefreshIntervalInSecs > 0)
            {
                refreshTimer = new System.Threading.Timer(
                    o => form?.Invoke(new Action(() => form.webView?.Reload())),
                    settings.Url,
                    TimeSpan.FromSeconds(1 + settings.RefreshIntervalInSecs),
                    TimeSpan.FromSeconds(settings.RefreshIntervalInSecs));
            }

            async void webViewDOMContentLoaded(object? s, EventArgs e)
            {
                // Try to auto-login on the web page with the specified credentials
                if (!String.IsNullOrWhiteSpace(settings.Username))
                {
                    var result = await form.webView.CoreWebView2.ExecuteScriptAsync(
                    "document.querySelector(\"input[type~='password']\") != null && " +
                    "window.getComputedStyle(document.querySelector(\"input[type~='password']\")).visibility != 'hidden'");
                    var isLoginPage = Boolean.Parse(result.Replace("\"", ""));
                    if (isLoginPage)
                    {
                        var assembly = typeof(Program).Assembly;
                        using var stream = assembly.GetManifestResourceStream("WebPageHost.AutoLogin.js");
                        using var reader = new StreamReader(stream);
                        var autoLoginJs = reader.ReadToEnd();
                        autoLoginJs = autoLoginJs.Replace("$(USERNAME)", settings.Username);
                        var passwordSpecified = !String.IsNullOrWhiteSpace(settings.Password);
                        autoLoginJs = autoLoginJs.Replace("$(PASSWORD)", passwordSpecified ? settings.Password : "");
                        await form.webView.CoreWebView2.ExecuteScriptAsync(autoLoginJs);
                        if (passwordSpecified)
                        {
                            // Simulate Enter key press outside of script because it would not be trusted otherwise
                            await Task.Delay(1000);
                            new InputSimulator().Keyboard.KeyPress(VirtualKeyCode.RETURN);
                        }
                    }
                }
            }
            form.WebViewDOMContentLoaded += webViewDOMContentLoaded;

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
            if (settings.WindowLocationArgument.Equals("Center", StringComparison.InvariantCultureIgnoreCase))
            {
                form.Bounds = new Rectangle
                {
                    X = monitor.WorkingArea.Left + (monitor.WorkingArea.Width - form.Width) / 2,
                    Y = monitor.WorkingArea.Top + (monitor.WorkingArea.Height - form.Height) / 2,
                    Width = form.Width,
                    Height = form.Height
                };
            }
            else if (settings.WindowLocationArgument.Equals("Last", StringComparison.InvariantCultureIgnoreCase))
            {
                form.Location = lastBounds.Location;
            }
            else
            {
                form.Location = new Point { X = monitor.WorkingArea.Left + settings.WindowLocation.X, Y = monitor.WorkingArea.Top + settings.WindowLocation.Y };
            }

            // Apply the specified window state argument
            form.WindowState = settings.WindowState;

            // Apply the specified top-most argument
            form.TopMost = settings.TopMost;

            // Show the UI
            Application.Run(form);

            if (null != refreshTimer)
            {
                refreshTimer.Dispose();
                refreshTimer = null;
            }

            // Clean-up when the application is about to close
            if (!settings.KeepUserData)
            {
                // Delete current user data folder
                DeleteWebView2UserDataFolder(userDataFolderName, form, logToStdout);
            }

            if (settings.HideConsole)
            {
                // Show the console window again on exit
                ShowWindow(consoleWindowHandle, SW_SHOW);
            }

            return 0;
        }

        /// <summary>
        /// Delete the WebView2 data folder for the current user.
        /// </summary>
        /// <param name="userDataFolderName">Folder name without path, relative to the folder of the program executable.</param>
        /// <param name="form">Referance to main form in order to access the WebView2 instance.</param>
        /// <param name="logToStdout">Specifies whether information logs shall be written to standard output.</param>
        private static void DeleteWebView2UserDataFolder(string userDataFolderName, MainForm form, bool logToStdout)
        {
            try
            {
                var exeFilePath = AppContext.BaseDirectory;
                var exeDirPath = Path.GetDirectoryName(exeFilePath);
                var dirInfo = new DirectoryInfo(exeDirPath);
                var userDataFolder = (DirectoryInfo)dirInfo.GetDirectories(userDataFolderName).GetValue(0);
                if (null != userDataFolder)
                {
                    if (logToStdout) AnsiConsole.Markup($"[grey]Cleaning-up user data...[/] ");

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
                        if (logToStdout) AnsiConsole.MarkupLine($"[grey]Done.[/] ");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Persistent store for the last recently used window bounds (stored in Windows Registry)
        /// </summary>
        public Rectangle LastWindowBounds
        {
            get
            {
                var bounds = new Rectangle();
                try
                {
                    // Read values from registry
                    var key = Registry.CurrentUser.OpenSubKey(Common.ProgramRegistryRootKeyPath);
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
                var key = Registry.CurrentUser.CreateSubKey(Common.ProgramRegistryRootKeyPath);
                key.SetValue("LastWindowPosX", lastWindowBounds.X);
                key.SetValue("LastWindowPosY", lastWindowBounds.Y);
                key.SetValue("LastWindowWidth", lastWindowBounds.Width);
                key.SetValue("LastWindowHeight", lastWindowBounds.Height);
                key.Close();
            }
        }
        private Rectangle lastWindowBounds;

        // Temporary storage for the custom result from the injected JavaScript
        private volatile string javaScriptResult = null;

        // Interop
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
    }
}
