// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;

namespace WebPageHost;

/// <summary>
/// Static helper functions used across commands.
/// </summary>
internal static class Helpers
{
    public static T Scale<T>(T value, float scaleFactor) where T : struct
    {
        if (value is int) {
            int val = (int)(object)value;
            val = (int)(val * scaleFactor);
            return (T)(object)val;
        }
        else if (typeof(T) == typeof(Point)) {
            var val = (Point)(object)value;
            val.X = (int)(val.X * scaleFactor);
            val.Y = (int)(val.Y * scaleFactor);
            return (T)(object)val;
        }
        else if (typeof(T) == typeof(Size)) {
            var val = (Size)(object)value;
            val.Width = (int)(val.Width * scaleFactor);
            val.Height = (int)(val.Height * scaleFactor);
            return (T)(object)val;
        }
        else if (typeof(T) == typeof(Rectangle)) {
            var val = (Rectangle)(object)value;
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
        if (str.Length > 0) {
            foreach (char c in str) {
                secureStr.AppendChar(c);
            }
        }
        return secureStr;
    }

    public static string ToUnSecureString(SecureString secstr)
    {
        if (null == secstr) {
            return string.Empty;
        }

        IntPtr unmanagedString = IntPtr.Zero;
        try {
            unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secstr);
            string? result = Marshal.PtrToStringUni(unmanagedString);
            if (result == null) {
                Trace.TraceWarning("SecureString could not be converted to an unsecure string");
                return string.Empty;
            }
            return result;
        }
        finally {
            Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
        }
    }
}
