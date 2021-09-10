using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;

namespace WebPageHost
{
    static class Helpers
    {
        public static T Scale<T>(T value, float scaleFactor) where T : struct
        {
            if (value is int)
            {
                int val = (int)(object)value;
                val = (int)(val * scaleFactor);
                return (T)(object)val;
            }
            else if (typeof(T) == typeof(Point))
            {
                Point val = (Point)(object)value;
                val.X = (int)(val.X * scaleFactor);
                val.Y = (int)(val.Y * scaleFactor);
                return (T)(object)val;
            }
            else if (typeof(T) == typeof(Size))
            {
                Size val = (Size)(object)value;
                val.Width = (int)(val.Width * scaleFactor);
                val.Height = (int)(val.Height * scaleFactor);
                return (T)(object)val;
            }
            else if (typeof(T) == typeof(Rectangle))
            {
                Rectangle val = (Rectangle)(object)value;
                val.X = (int)(val.X * scaleFactor);
                val.Y = (int)(val.Y * scaleFactor);
                val.Width = (int)(val.Width * scaleFactor);
                val.Height = (int)(val.Height * scaleFactor);
                return (T)(object)val;
            }
            return default;
        }

        public static SecureString ToSecureString(string str)
        {
            var secureStr = new SecureString();
            if (str.Length > 0)
            {
                foreach (var c in str) secureStr.AppendChar(c);
            }
            return secureStr;
        }

        public static string ToUnSecureString(SecureString secstr)
        {
            if (null == secstr) return String.Empty;
            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secstr);
                var result = Marshal.PtrToStringUni(unmanagedString);
                if (result == null)
                {
                    Trace.TraceWarning("SecureString could not be converted to an unsecure string");
                    return String.Empty;
                }
                return result;
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
    }
}
