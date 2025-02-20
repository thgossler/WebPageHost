// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System;

namespace WebPageHost;

/// <summary>
/// Common definitions used across commands.
/// </summary>
internal static class Common
{
    public static string ProgramName = "WebPageHost";
    public static string ProgramRegistryRootKeyPath = @"SOFTWARE\WebPageHost";

    public static string WebView2UserDataFolderName {
        get => Environment.UserName + ".WebView2";
        private set { }
    }
}
