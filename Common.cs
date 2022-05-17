using System;

namespace WebPageHost
{
    internal static class Common
    {
        public static string ProgramRegistryRootKeyPath = @"SOFTWARE\WebPageHost";

        public static string WebView2UserDataFolderName {
            get { 
                return Environment.UserName + ".WebView2";
            }
            private set { }
        }  
    }
}
