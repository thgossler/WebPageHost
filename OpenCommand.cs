// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

#nullable disable warnings

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Spectre.Console;
using Spectre.Console.Cli;
using WindowsInput;
using WindowsInput.Native;
using static WebPageHost.ScreenExtensions;
using Size = System.Drawing.Size;

namespace WebPageHost;

/// <summary>
/// Opens a URL in an embedded WebView2 control.
/// </summary>
internal sealed partial class OpenCommand : Command<OpenCommand.Settings>
{
    /// <summary>
    /// Open command handler
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The provided command line arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Program exit code</returns>
    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings, CancellationToken cancellationToken)
    {
        if (settings.LaunchWithoutWaiting) {
            // Execute the process again with exactly the same command line arguments and close this instance immediately
            var cmdLine = Environment.CommandLine.ToLower().Replace("-c", "").Replace("--continue", "");
            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = Process.GetCurrentProcess().MainModule.FileName,
                Arguments = cmdLine.Substring(cmdLine.IndexOf("open")),
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true
            };
            _ = Process.Start(startInfo);
            return 0;
        }

        // If a script for custom output is specified then do not output other logs
        bool logToStdout = string.IsNullOrWhiteSpace(settings.ResultJavaScript);

        if (logToStdout) {
            AnsiConsole.MarkupLine($"Opening URL [green]{settings.Url}[/]...");
        }

        // Initialize WinForms
        _ = Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetColorMode(SystemColorMode.System);

        IntPtr consoleWindowHandle = Win32Interop.GetConsoleWindow();
        if (settings.HideConsole) {
            // Hide the console window now after start and argument processing done
            _ = Win32Interop.ShowWindow(consoleWindowHandle, Win32Interop.SW_HIDE);
        }

        // Determine monitor placement
        Screen monitor = settings.MonitorNumber == -1 ? Screen.PrimaryScreen : Screen.AllScreens[settings.MonitorNumber];
        Point dpi = ScreenExtensions.GetMonitorDpi(monitor, DpiType.Effective);
        float scaleFactor = (float)dpi.X / 96;

        // Get WebView2 user folder name
        string userDataFolderName = Common.WebView2UserDataFolderName;
        var envName = string.Empty;
        if (!string.IsNullOrWhiteSpace(settings.EnvironmentName)) {
            envName = settings.EnvironmentName.Replace(" ", "").Replace("\\", "").Replace("/", "").Replace(".", "").Replace("*", "").Replace("?", "");
            userDataFolderName = $"{userDataFolderName}-{envName}";
        }

        var form = new MainForm(userDataFolderName, settings.Url, settings.WindowTitle, settings.DisableSingleSignOnUsingOSPrimaryAccount, settings.SuppressCertErrors, settings.ForceDarkMode);
        form.FormLoaded += (s, e) => {
            // Apply the specified zoom factor
            var form = (MainForm)s;
            form.WebView.ZoomFactor = (double)settings.ZoomFactor;
        };
        form.FormClosing += async (s, e) => {
            // Get a custom exit result by executing the provided JavaScript statement on the web page
            if (!string.IsNullOrWhiteSpace(settings.ResultJavaScript) && null == javaScriptResult) {
                javaScriptResult = string.Empty;
                e.Cancel = true;
                javaScriptResult = await form.WebView.ExecuteScriptAsync(settings.ResultJavaScript);
                AnsiConsole.WriteLine(javaScriptResult);
                form.Close();
            }

            // Remember window bounds for the next launch
            _ = SaveLastWindowBoundsToRegistry(form.Bounds, envName);

            // Print the current browser URL to stdout for external processing
            string lastUrl = form.Url;
            if (logToStdout) {
                AnsiConsole.MarkupLine($"LastUrl=[green]{lastUrl}[/]");
            }
        };

        // Refresh the current web page after the specified interval
        System.Threading.Timer refreshTimer = null;
        if (settings.RefreshIntervalInSecs > 0) {
            refreshTimer = new System.Threading.Timer(
                o => form?.Invoke(new Action(() => form.WebView?.Reload())),
                settings.Url,
                TimeSpan.FromSeconds(1 + settings.RefreshIntervalInSecs),
                TimeSpan.FromSeconds(settings.RefreshIntervalInSecs));
        }

        async void webViewDOMContentLoaded(object? s, EventArgs e)
        {
            // Try to auto-login on the web page with the specified credentials
            if (!string.IsNullOrWhiteSpace(settings.Username)) {
                string result = await form.WebView.CoreWebView2.ExecuteScriptAsync(
                "document.querySelector(\"input[type~='password']\") != null && " +
                "window.getComputedStyle(document.querySelector(\"input[type~='password']\")).visibility != 'hidden'");
                bool isLoginPage = bool.Parse(result.Replace("\"", ""));
                if (isLoginPage) {
                    System.Reflection.Assembly assembly = typeof(Program).Assembly;
                    using Stream stream = assembly.GetManifestResourceStream("WebPageHost.AutoLogin.js");
                    using var reader = new StreamReader(stream);
                    string autoLoginJs = reader.ReadToEnd();
                    autoLoginJs = autoLoginJs.Replace("$(USERNAME)", settings.Username);
                    bool passwordSpecified = !string.IsNullOrWhiteSpace(settings.Password);
                    autoLoginJs = autoLoginJs.Replace("$(PASSWORD)", passwordSpecified ? settings.Password : "");
                    _ = await form.WebView.CoreWebView2.ExecuteScriptAsync(autoLoginJs);
                    if (passwordSpecified) {
                        // Simulate Enter key press outside of script because it would not be trusted otherwise
                        await Task.Delay(1000);
                        _ = new InputSimulator().Keyboard.KeyPress(VirtualKeyCode.RETURN);
                    }
                }
            }
        }
        form.WebViewDOMContentLoaded += webViewDOMContentLoaded;

        // Apply the specified window location and size arguments
        bool useLastPos = settings.WindowLocationArgument.Equals("Last", StringComparison.InvariantCultureIgnoreCase);
        bool useLastSize = settings.WindowSizeArgument.Equals("Last", StringComparison.InvariantCultureIgnoreCase);
        Rectangle? lastBounds = null;
        if (useLastPos || useLastSize) {
            // Load the window bounds from the last session
            lastBounds = LoadLastWindowsBoundsFromRegistry(envName);
        }
        form.StartPosition = FormStartPosition.Manual;

        // Apply the specified window state argument
        form.WindowState = settings.WindowState;

        // Apply the specified border style argument
        form.FormBorderStyle = settings.BorderStyle;

        var location = useLastPos ? lastBounds.Value.Location :
            new Point { X = monitor.WorkingArea.Left + settings.WindowLocation.X, Y = monitor.WorkingArea.Top + settings.WindowLocation.Y };

        var size = useLastSize ? lastBounds.Value.Size : Helpers.Scale(settings.WindowSize, scaleFactor);

        if (settings.WindowLocationArgument.Equals("Center", StringComparison.InvariantCultureIgnoreCase)) {
            form.Bounds = new Rectangle {
                X = monitor.WorkingArea.Left + ((monitor.WorkingArea.Width - size.Width) / 2),
                Y = monitor.WorkingArea.Top + ((monitor.WorkingArea.Height - size.Height) / 2),
                Width = size.Width,
                Height = size.Height
            };
        }
        else {
            form.Bounds = new Rectangle {
                Location = location,
                Size = size
            };
        }

        // Apply the specified top-most argument
        form.TopMost = settings.TopMost;

        // Show button eventually
        if (!string.IsNullOrWhiteSpace(settings.ShowButtonWithTitle)) {
            form.ButtonTitle = settings.ShowButtonWithTitle;
        }

        // Show the UI
        Application.Run(form);

        if (null != refreshTimer) {
            refreshTimer.Dispose();
            refreshTimer = null;
        }

        // Clean-up when the application is about to close
        if (!settings.KeepUserData) {
            // Delete current user data folder
            DeleteWebView2UserDataFolder(userDataFolderName, form, logToStdout);
        }

        if (settings.HideConsole) {
            // Show the console window again on exit
            _ = Win32Interop.ShowWindow(consoleWindowHandle, Win32Interop.SW_SHOW);
        }

        return 0;
    }

    // Temporary storage for the custom result from the injected JavaScript
    private volatile string javaScriptResult;

    /// <summary>
    /// Delete the WebView2 data folder for the current user.
    /// </summary>
    /// <param name="userDataFolderName">Folder name without path, relative to the folder of the program executable.</param>
    /// <param name="form">Referance to main form in order to access the WebView2 instance.</param>
    /// <param name="logToStdout">Specifies whether information logs shall be written to standard output.</param>
    private static void DeleteWebView2UserDataFolder(string userDataFolderName, MainForm form, bool logToStdout)
    {
        try {
            var userDataFolder = new DirectoryInfo(MainForm.UserDataFolderPath);
            if (null != userDataFolder) {
                if (logToStdout) {
                    AnsiConsole.Markup($"[grey]Cleaning-up user data...[/] ");
                }

                form.WebView.Dispose(); // Ensure external browser process is exited

                bool success = false;
                int attempts = 0;
                while (!success && attempts <= 5) {
                    attempts++;
                    try {
                        userDataFolder.Delete(true);
                        if (!userDataFolder.Exists) {
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
                if (!success) {
                    Trace.TraceWarning(string.Format("ERROR: WebView2 user data folder '{0}' could not be deleted", userDataFolder.FullName));
                }
                else {
                    if (logToStdout) {
                        AnsiConsole.MarkupLine($"[grey]Done.[/] ");
                    }
                }
            }
        }
        catch (Exception ex) {
            Debug.WriteLine(ex.Message);
        }
    }

    /// <summary>
    /// Saves the last window bounds to Windows registry.
    /// </summary>
    /// <param name="lastWindowBounds">The last window bounds.</param>
    /// <returns>True if successfully written, false otherwise.</returns>
    private static bool SaveLastWindowBoundsToRegistry(Rectangle lastWindowBounds, string envName = "")
    {
        try {
            string l = System.Text.Json.JsonSerializer.Serialize<Point>(lastWindowBounds.Location, new System.Text.Json.JsonSerializerOptions { IgnoreReadOnlyProperties = true });
            string s = System.Text.Json.JsonSerializer.Serialize<Size>(lastWindowBounds.Size, new System.Text.Json.JsonSerializerOptions { IgnoreReadOnlyProperties = true });
            var keyPath = Common.ProgramRegistryRootKeyPath;
            if (!string.IsNullOrWhiteSpace(envName)) {
                keyPath = $"{keyPath}-{envName}";
            }
            RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath, true);
            key.SetValue("LastWindowLocation", l);
            key.SetValue("LastWindowSize", s);
            key.Close();
        }
        catch (Exception ex) {
            Trace.TraceError("Last window bounds could not be written to registry, exception: ", ex);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Loads the last window bounds from Windows registry.
    /// </summary>
    /// <returns>The read last window bounds if successful, default bounds otherwise.</returns>
    private static Rectangle LoadLastWindowsBoundsFromRegistry(string envName = "")
    {
        var bounds = new Rectangle { X = 0, Y = 0, Width = 1280, Height = 720 };
        try {
            string l = null;
            string s = null;
            var keyPath = Common.ProgramRegistryRootKeyPath;
            if (!string.IsNullOrWhiteSpace(envName)) {
                keyPath = $"{keyPath}-{envName}";
            }
            RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath);
            if (null != key) {
                l = (string)key.GetValue("LastWindowLocation");
                s = (string)key.GetValue("LastWindowSize");
            }
            key.Close();
            if (!string.IsNullOrWhiteSpace(l)) {
                bounds.Location = System.Text.Json.JsonSerializer.Deserialize<Point>(l);
            }
            if (!string.IsNullOrWhiteSpace(s)) {
                bounds.Size = System.Text.Json.JsonSerializer.Deserialize<Size>(s);
            }
        }
        catch (Exception ex) {
            Trace.TraceError("Last window bounds could not be read from registry, exception: ", ex);
        }
        return bounds;
    }
}
