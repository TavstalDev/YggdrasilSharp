using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using Tavstal.YggdrasilSharp.Models.Common;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Tavstal.YggdrasilSharp.Models.Database.User;

/// <summary>
/// Represents a custom user login that extends the IdentityUserLogin class.
/// Includes additional properties such as IP addresses, location, and device information.
/// </summary>
public sealed class CustomUserLogin : IdentityUserLogin<string>
{
    /// <summary>
    /// Gets or sets the unique identifier for the user login.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }
    
    /// <summary>
    /// Gets or sets the user ID associated with the login.
    /// </summary>
    [StringLength(36)]
    public override string UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the key provided by the login provider.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [StringLength(255)]
    [JsonIgnore]
    public override string ProviderKey { get; set; }
    
    /// <summary>
    /// Gets or sets the display name of the login provider.
    /// </summary>
    [StringLength(32)]
    public override string? ProviderDisplayName { get; set; }
    
    /// <summary>
    /// Gets or sets the login provider name.
    /// </summary>
    [StringLength(64)]
    public override string LoginProvider { get; set; }
    
    /// <summary>
    /// Gets or sets the IPv4 address of the user during login.
    /// </summary>
    [StringLength(15)]
    public string? IPv4Address { get; set; }
    
    /// <summary>
    /// Gets or sets the IPv6 address of the user during login.
    /// </summary>
    [StringLength(40)]
    public string? IPv6Address { get; set; }
    
    /// <summary>
    /// Gets or sets the country of the user during login.
    /// </summary>
    [StringLength(256)]
    public string? Country { get; set; }
    
    /// <summary>
    /// Gets or sets the city of the user during login.
    /// </summary>
    [StringLength(256)]
    public string? City { get; set; }
    
    /// <summary>
    /// Gets or sets the region of the user during login.
    /// </summary>
    [StringLength(256)]
    public string? Region { get; set; }
    
    /// <summary>
    /// Gets or sets the operating system of the user during login.
    /// </summary>
    [StringLength(256)]
    public string? OperatingSystem { get; set; }
    
    /// <summary>
    /// Gets or sets the browser used by the user during login.
    /// </summary>
    [StringLength(256)]
    public string? Browser { get; set; }
    
    /// <summary>
    /// Gets or sets the creation date of the login record.
    /// </summary>
    public DateTimeOffset CreateDate { get; set; }
    
    /// <summary>
    /// Gets or sets the expiration date of the login record.
    /// </summary>
    public DateTimeOffset ExpireDate { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomUserLogin"/> class.
    /// </summary>
    public CustomUserLogin() {}
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomUserLogin"/> class with the specified properties.
    /// </summary>
    /// <param name="userId">The user ID associated with the login.</param>
    /// <param name="providerKey">The key provided by the login provider.</param>
    /// <param name="providerDisplayName">The display name of the login provider.</param>
    /// <param name="loginProvider">The login provider name.</param>
    /// <param name="ipv4Address">The IPv4 address of the user during login.</param>
    /// <param name="ipv6Address">The IPv6 address of the user during login.</param>
    /// <param name="country">The country of the user during login.</param>
    /// <param name="city">The city of the user during login.</param>
    /// <param name="region">The region of the user during login.</param>
    /// <param name="operatingSystem">The operating system of the user during login.</param>
    /// <param name="browser">The browser used by the user during login.</param>
    /// <param name="createDate">The creation date of the login record.</param>
    /// <param name="expireDate">The expiration date of the login record.</param>
    public CustomUserLogin(string userId, string providerKey, string providerDisplayName, string loginProvider, string ipv4Address, string ipv6Address, string country, string city, string region, string operatingSystem, string browser, DateTimeOffset createDate, DateTimeOffset expireDate)
    {
        UserId = userId;
        ProviderKey = providerKey;
        ProviderDisplayName = providerDisplayName;
        LoginProvider = loginProvider;
        IPv4Address = ipv4Address;
        IPv6Address = ipv6Address;
        Country = country;
        City = city;
        Region = region;
        OperatingSystem = operatingSystem;
        Browser = browser;
        CreateDate = createDate;
        ExpireDate = expireDate;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomUserLogin"/> class with the specified properties.
    /// </summary>
    /// <param name="userId">The user ID associated with the login.</param>
    /// <param name="providerKey">The key provided by the login provider.</param>
    /// <param name="providerDisplayName">The display name of the login provider.</param>
    /// <param name="loginProvider">The login provider name.</param>
    /// <param name="ipv4Address">The IPv4 address of the user during login.</param>
    /// <param name="ipv6Address">The IPv6 address of the user during login.</param>
    /// <param name="ipInfo">The IP information of the user during login.</param>
    /// <param name="operatingSystem">The operating system of the user during login.</param>
    /// <param name="browser">The browser used by the user during login.</param>
    /// <param name="createDate">The creation date of the login record.</param>
    /// <param name="expireDate">The expiration date of the login record.</param>
    public CustomUserLogin(string userId, string providerKey, string providerDisplayName, string loginProvider, string? ipv4Address, string? ipv6Address, IpInfo ipInfo, string operatingSystem, string browser, DateTimeOffset createDate, DateTimeOffset expireDate)
    {
        UserId = userId;
        ProviderKey = providerKey;
        ProviderDisplayName = providerDisplayName;
        LoginProvider = loginProvider;
        IPv4Address = ipv4Address;
        IPv6Address = ipv6Address;
        Country = ipInfo.Country;
        City = ipInfo.City;
        Region = ipInfo.Region;
        OperatingSystem = operatingSystem;
        Browser = browser;
        CreateDate = createDate;
        ExpireDate = expireDate;
    }

    /* ######################################################################
     *                         NAVIGATION PROPERTIES
     * ###################################################################### */

    /// <summary>
    /// Gets or sets the user associated with the login.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [ForeignKey("UserId")]
    [JsonIgnore]
    public CustomUser? User { get; set; }
}
