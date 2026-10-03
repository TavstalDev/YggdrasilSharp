using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Common;

/// <summary>
/// Represents information about an IP address, including its location and organization details.
/// </summary>
public class IpInfo
{
    /// <summary>
    /// Gets or sets the IP address.
    /// </summary>
    [JsonPropertyName("ip")]
    public string Ip { get; set; }
    
    /// <summary>
    /// Gets or sets the hostname associated with the IP address.
    /// </summary>
    [JsonPropertyName("hostname")]
    public string Hostname { get; set; }
    
    /// <summary>
    /// Gets or sets the city where the IP address is located.
    /// </summary>
    [JsonPropertyName("city")]
    public string City { get; set; }
    
    /// <summary>
    /// Gets or sets the region where the IP address is located.
    /// </summary>
    [JsonPropertyName("region")]
    public string Region { get; set; }
    
    /// <summary>
    /// Gets or sets the country where the IP address is located.
    /// </summary>
    [JsonPropertyName("country")]
    public string Country { get; set; }
    
    /// <summary>
    /// Gets or sets the latitude and longitude coordinates of the IP address location.
    /// </summary>
    [JsonPropertyName("loc")]
    public string Loc { get; set; }
    
    /// <summary>
    /// Gets or sets the organization associated with the IP address.
    /// </summary>
    [JsonPropertyName("org")]
    public string Organization { get; set; }
    
    /// <summary>
    /// Gets or sets the postal code of the IP address location.
    /// </summary>
    [JsonPropertyName("postal")]
    public string Postal { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="IpInfo"/> class with the specified details.
    /// </summary>
    /// <param name="ip">The IP address.</param>
    /// <param name="hostname">The hostname associated with the IP address.</param>
    /// <param name="city">The city where the IP address is located.</param>
    /// <param name="region">The region where the IP address is located.</param>
    /// <param name="country">The country where the IP address is located.</param>
    /// <param name="loc">The latitude and longitude coordinates of the IP address location.</param>
    /// <param name="organization">The organization associated with the IP address.</param>
    /// <param name="postal">The postal code of the IP address location.</param>
    public IpInfo(string ip, string hostname, string city, string region, string country, string loc, string organization, string postal)
    {
        Ip = ip;
        Hostname = hostname;
        City = city;
        Region = region;
        Country = country;
        Loc = loc;
        Organization = organization;
        Postal = postal;
    }
}
