using System.Net;

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

    /// <summary>
    /// Gets the IP address of the client that issued the request.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>The normalized client IP address, or null when unavailable.</returns>
    public static string? GetClientIp(HttpContext httpContext)
    {
        return NormalizeIp(httpContext.Connection.RemoteIpAddress?.ToString());
    }

    /// <summary>
    /// Normalizes an IP address string so that equivalent forms compare equal.
    /// </summary>
    /// <param name="ip">The address to normalize. May be null, empty, or malformed.</param>
    /// <returns>The normalized address, or null when the input is absent or not a valid IP address.</returns>
    public static string? NormalizeIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return null;

        if (!IPAddress.TryParse(ip.Trim(), out IPAddress? address))
            return null;

        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        return address.ToString();
    }
}