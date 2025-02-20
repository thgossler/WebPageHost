// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

#nullable disable warnings

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security;
using Microsoft.Win32;
using Spectre.Console;
using Spectre.Console.Cli;

namespace WebPageHost;

/// <summary>
/// Cleans-up user resources in file system and Windows registry.
/// </summary>
internal sealed partial class CleanupCommand : Command<CleanupCommandSettings>
{
    /// <summary>
    /// Cleanup command handler
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The provided command line arguments.</param>
    /// <returns>Program exit code</returns>
    public override int Execute([NotNull] CommandContext context, [NotNull] CleanupCommandSettings settings)
    {
        // Get WebView2 user folder name
        string userDataFolderName = Common.WebView2UserDataFolderName;
        var envName = string.Empty;
        if (!string.IsNullOrWhiteSpace(settings.EnvironmentName)) {
            envName = settings.EnvironmentName.Replace(" ", "").Replace("\\", "").Replace("/", "").Replace(".", "").Replace("*", "").Replace("?", "");
            userDataFolderName = $"{userDataFolderName}-{envName}";
        }

        // Delete WebView2 data folder of the current user
        AnsiConsole.Markup($"Removing the current user's web browser persistent data folder... ");
        DeleteWebView2UserDataFolder(userDataFolderName);
        AnsiConsole.MarkupLine($"[green]Done[/].");

        // Delete program registry seetings for the current user
        AnsiConsole.Markup($"Removing the current user's registry settings for this program... ");
        DeleteRegistrySettings(envName);
        AnsiConsole.MarkupLine($"[green]Done[/].");

        return 0;
    }

    /// <summary>
    /// Delete the WebView2 data folder for the current user.
    /// </summary>
    /// <param name="userDataFolderName">Folder name without path, relative to the folder of the program executable.</param>
    private static void DeleteWebView2UserDataFolder(string userDataFolderName)
    {
        try {
            var userDataFolder = new DirectoryInfo(MainForm.UserDataFolderPath);
            if (null != userDataFolder) {
                bool success = false;
                try {
                    if (userDataFolder.Exists) {
                        userDataFolder.Delete(true);
                    }
                    success = !userDataFolder.Exists;
                }
                catch (UnauthorizedAccessException) { }
                catch (DirectoryNotFoundException) { }
                catch (IOException) { }
                catch (SecurityException) { }
                if (!success) {
                    Trace.TraceWarning(string.Format("ERROR: WebView2 user data folder '{0}' could not be deleted", userDataFolder.FullName));
                }
            }
        }
        catch (Exception ex) {
            Debug.WriteLine(ex.Message);
        }
    }

    /// <summary>
    /// Delete the program registry settings for the current user.
    /// </summary>
    private static void DeleteRegistrySettings(string envName = "")
    {
        try {
            var keyPath = Common.ProgramRegistryRootKeyPath;
            if (!string.IsNullOrWhiteSpace(envName)) {
                keyPath = $"{keyPath}-{envName}";
            }
            Registry.CurrentUser.DeleteSubKeyTree(Common.ProgramRegistryRootKeyPath);
        }
        catch (Exception ex) {
            Debug.WriteLine(ex.Message);
        }
    }
}
