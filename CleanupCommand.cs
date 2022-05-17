#nullable disable warnings

using Microsoft.Win32;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace WebPageHost
{
    internal sealed partial class CleanupCommand : Command<CleanupCommandSettings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] CleanupCommandSettings settings)
        {
            // Define WebView2 user folder name
            var userDataFolderName = Common.WebView2UserDataFolderName;

            // Delete current user data folder
            AnsiConsole.Markup($"Removing the current user's web browser persistent data folder... ");
            DeleteWebView2UserDataFolder(userDataFolderName);
            AnsiConsole.MarkupLine($"[green]Done[/].");

            AnsiConsole.Markup($"Removing the current user's registry settings for this program... ");
            DeleteRegistrySettings();
            AnsiConsole.MarkupLine($"[green]Done[/].");

            return 0;
        }

        private static void DeleteWebView2UserDataFolder(string userDataFolderName)
        {
            try
            {
                var exeFilePath = AppContext.BaseDirectory;
                var exeDirPath = Path.GetDirectoryName(exeFilePath);
                var dirInfo = new DirectoryInfo(exeDirPath);
                var userDataFolder = (DirectoryInfo)dirInfo.GetDirectories(userDataFolderName).GetValue(0);
                if (null != userDataFolder)
                {
                    bool success = false;
                    try
                    {
                        if (userDataFolder.Exists)
                        {
                            userDataFolder.Delete(true);
                        }
                        success = !userDataFolder.Exists;
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (DirectoryNotFoundException) { }
                    catch (IOException) { }
                    catch (SecurityException) { }
                    if (!success)
                    {
                        Trace.TraceWarning(String.Format("ERROR: WebView2 user data folder '{0}' could not be deleted", userDataFolder.FullName));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void DeleteRegistrySettings()
        {
            // Delete values from registry
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(Common.ProgramRegistryRootKeyPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        // Interop
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
    }

    public sealed class CleanupCommandSettings : CommandSettings
    {
    }
}
