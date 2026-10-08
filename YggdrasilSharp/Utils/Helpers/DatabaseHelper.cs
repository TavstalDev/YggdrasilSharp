using System.Globalization;
using System.Text.Json;
using Tavstal.YggdrasilSharp.Models.Common;
using Tavstal.YggdrasilSharp.Serialization;

namespace Tavstal.YggdrasilSharp.Utils.Helpers;

/// <summary>
/// Provides helper methods for database-related operations
/// </summary>
public static class DatabaseHelper
{
    private static readonly HttpClient _client = new();

    /// <summary>
    /// Retrieves information about an IP address, such as country, region, and city.
    /// If the information cannot be retrieved, returns default values.
    /// </summary>
    /// <param name="ip">The IP address to retrieve information for.</param>
    /// <returns>An <see cref="IpInfo"/> object containing the IP information.</returns>
    public static async Task<IpInfo> GetIpInformation(string ip)
    {
        IpInfo ipInfo = new IpInfo(
            ip,
            "Unknown",
            "Unknown",
            "Unknown",
            "Unknown",
            "0.0 0.0",
            "Unknown",
            "Unknown"
        );

        try
        {
            string info = await _client.GetStringAsync($"https://ipinfo.io/{ip}");
            var localInfo = JsonSerializer.Deserialize(info, CustomJsonContext.Default.IpInfo);
            if (localInfo != null)
            {
                ipInfo = localInfo;
                ipInfo.Country = new RegionInfo(ipInfo.Country).EnglishName;
            }
        }
        catch (Exception)
        {
            // Ignore
        }
        return ipInfo;
    }
}
