using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Config;

/// <summary>
/// Represents the reverse proxy / trusted proxy configuration loaded from the configuration.
/// </summary>
public class ProxyConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether forwarded headers are processed.
    /// Must stay <c>false</c> when the app is exposed directly to the internet.
    /// </summary>
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the number of proxy hops that are allowed to append to <c>X-Forwarded-For</c>.
    /// Use 1 for a single reverse proxy, 2 for CDN + reverse proxy.
    /// </summary>
    [JsonPropertyName("ForwardLimit")]
    public int ForwardLimit { get; set; } = 1;

    /// <summary>
    /// Gets or sets the exact addresses of the proxies allowed to send forwarding headers.
    /// </summary>
    [JsonPropertyName("KnownProxies")]
    public string[] KnownProxies { get; set; } = [];

    /// <summary>
    /// Gets or sets the CIDR ranges of the proxies allowed to send forwarding headers.
    /// </summary>
    [JsonPropertyName("KnownNetworks")]
    public string[] KnownNetworks { get; set; } = [];

    /// <summary>
    /// Gets or sets the host names allowed in the forwarding headers.
    /// </summary>
    [JsonPropertyName("AllowedHosts")]
    public string[] AllowedHosts { get; set; } = [];
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ProxyConfig"/> class.
    /// </summary>
    public ProxyConfig() {}

    /// <summary>
    /// Initializes a new instance of the <see cref="ProxyConfig"/> class and loads settings from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration to load settings from.</param>
    public ProxyConfig(IConfiguration configuration)
    {
        Enabled = configuration.GetValue(Constants.ConfigurationKeys.ProxyEnabled, false);
        ForwardLimit = configuration.GetValue(Constants.ConfigurationKeys.ProxyForwardLimit, 1);
        KnownProxies = configuration.GetSection(Constants.ConfigurationKeys.ProxyKnownProxies).Get<string[]>() ?? [];
        KnownNetworks = configuration.GetSection(Constants.ConfigurationKeys.ProxyKnownNetworks).Get<string[]>() ?? [];
        AllowedHosts = configuration.GetSection(Constants.ConfigurationKeys.ProxyAllowedHosts).Get<string[]>() ?? [];
    }
}
