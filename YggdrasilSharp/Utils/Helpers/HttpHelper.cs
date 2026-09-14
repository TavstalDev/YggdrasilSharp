namespace Tavstal.YggdrasilSharp.Utils.Helpers;

/// <summary>
/// Provides helper methods for HTTP-related operations.
/// </summary>
public static class HttpHelper
{
    /// <summary>
    /// Determines the operating system from the user agent string.
    /// </summary>
    /// <param name="userAgent">The user agent string.</param>
    /// <returns>The name of the operating system.</returns>
    public static string GetOperatingSystem(string userAgent)
    {
        if (userAgent.Contains("Windows NT")) return "Windows";
        if (userAgent.Contains("Mac OS X")) return "MacOS";
        if (userAgent.Contains("Linux")) return "Linux";
        return "Unknown";
    }

    /// <summary>
    /// Determines the browser from the user agent string.
    /// </summary>
    /// <param name="userAgent">The user agent string.</param>
    /// <returns>The name of the browser.</returns>
    public static string GetBrowser(string userAgent)
    {
        if (userAgent.Contains("Chrome")) return "Chrome";
        if (userAgent.Contains("Firefox")) return "Firefox";
        if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome")) return "Safari";
        if (userAgent.Contains("Edge")) return "Edge";
        if (userAgent.Contains("MSIE") || userAgent.Contains("Trident")) return "Internet Explorer";
        return "Unknown";
    }
}