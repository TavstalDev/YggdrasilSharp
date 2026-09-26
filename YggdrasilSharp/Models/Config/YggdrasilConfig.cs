using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Config;

/// <summary>
/// Represents the Yggdrasil-compatible server settings loaded from the configuration.
/// </summary>
public class YggdrasilConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether the request IP is compared against the
    /// play session IP when a client joins a game server.
    /// </summary>
    [JsonPropertyName("EnforceIpCheckInJoin")]
    public bool EnforceIpCheckInJoin { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the IP reported by the game server is compared
    /// against the stored join IP on hasJoined requests.
    /// </summary>
    [JsonPropertyName("EnforceIpCheckInHasJoined")]
    public bool EnforceIpCheckInHasJoined { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether hasJoined requests may omit the IP address.
    /// </summary>
    [JsonPropertyName("AllowEmptyJoinedAddress")]
    public bool AllowEmptyJoinedAddress { get; set; }
    
    /// <summary>
    /// Gets or sets the allowed skin domains.
    /// </summary>
    public string[] SkinDomains { get; set; }

    /// <summary>
    /// Gets or sets the server name.
    /// </summary>
    public string ServerName { get; set; }

    /// <summary>
    /// Gets or sets the implementation name.
    /// </summary>
    public string ImplementationName { get; set; }

    /// <summary>
    /// Gets or sets the implementation version.
    /// </summary>
    public string ImplementationVersion { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YggdrasilConfig"/> class with explicit values.
    /// </summary>
    /// <param name="enforceIpCheckInJoin">Whether the request IP is compared against the play session IP when joining a server.</param>
    /// <param name="enforceIpCheckInHasJoined">Whether the IP reported by the game server is compared against the stored join IP on hasJoined requests.</param>
    /// <param name="allowEmptyJoinedAddress">Whether hasJoined requests may omit the IP address.</param>
    /// <param name="skinDomains">An array of allowed domains for serving skins.</param>
    /// <param name="serverName">The server name presented by Yggdrasil-compatible endpoints.</param>
    /// <param name="implementationName">The name of the Yggdrasil implementation (metadata shown to clients).</param>
    /// <param name="implementationVersion">The version string of the Yggdrasil implementation.</param>
    public YggdrasilConfig(bool enforceIpCheckInJoin, bool enforceIpCheckInHasJoined, bool allowEmptyJoinedAddress, string[] skinDomains, string serverName, string implementationName, string implementationVersion)
    {
        EnforceIpCheckInJoin = enforceIpCheckInJoin;
        EnforceIpCheckInHasJoined = enforceIpCheckInHasJoined;
        AllowEmptyJoinedAddress = allowEmptyJoinedAddress;
        SkinDomains = skinDomains;
        ServerName = serverName;
        ImplementationName = implementationName;
        ImplementationVersion = implementationVersion;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YggdrasilConfig"/> class and loads settings from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration to load settings from.</param>
    /// <exception cref="InvalidOperationException">Thrown if a required Yggdrasil configuration value is missing.</exception>
    public YggdrasilConfig(IConfiguration configuration)
    {
        EnforceIpCheckInJoin = configuration.GetValue(Constants.ConfigurationKeys.YggdrasilEnforceIpCheckInJoin, true);
        EnforceIpCheckInHasJoined = configuration.GetValue(Constants.ConfigurationKeys.YggdrasilEnforceIpCheckInHasJoined, true);
        AllowEmptyJoinedAddress = configuration.GetValue(Constants.ConfigurationKeys.YggdrasilAllowEmptyJoinedAddress, false);
        SkinDomains = configuration.GetSection(Constants.ConfigurationKeys.YggdrasilSkinDomains).Get<string[]>() ?? throw new InvalidOperationException(Constants.ConfigurationKeys.YggdrasilSkinDomains);
        ServerName = AppConfiguration.GetString(configuration, Constants.ConfigurationKeys.YggdrasilServerName);
        ImplementationName = AppConfiguration.GetString(configuration, Constants.ConfigurationKeys.YggdrasilImplementationName);
        ImplementationVersion = AppConfiguration.GetString(configuration, Constants.ConfigurationKeys.YggdrasilImplementationVersion);
    }
}