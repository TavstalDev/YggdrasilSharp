using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Config;

/// <summary>
/// Represents the Yggdrasil-compatible server settings loaded from the configuration.
/// </summary>
public class YggdrasilConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether the legacy Yggdrasil authentication flow
    /// is enabled alongside the JWT-based flow.
    /// </summary>
    [JsonPropertyName("EnableLegacyAuth")]
    public bool EnableLegacyAuth { get; set; }

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
    /// Gets or sets a value indicating whether the AuthController received agent
    /// is validated against <see cref="Agent"/>. When disabled, any agent is accepted.
    /// </summary>
    [JsonPropertyName("EnforceAgent")]
    public bool EnforceAgent { get; set; }

    /// <summary>
    /// Gets or sets the user agent accepted by the session server endpoints.
    /// </summary>
    [JsonPropertyName("Agent")]
    public AgentConfig Agent { get; set; }

    /// <summary>
    /// Gets or sets the allowed skin domains.
    /// </summary>
    [JsonPropertyName("SkinDomains")]
    public string[] SkinDomains { get; set; }

    /// <summary>
    /// Gets or sets the server identifiers that clients are not allowed to join.
    /// Returned verbatim by the session server's <c>blockedservers</c> endpoint.
    /// </summary>
    [JsonPropertyName("BlockedServers")]
    public string[] BlockedServers { get; set; }

    /// <summary>
    /// Gets or sets the server name.
    /// </summary>
    [JsonPropertyName("ServerName")]
    public string ServerName { get; set; }

    /// <summary>
    /// Gets or sets the implementation name.
    /// </summary>
    [JsonPropertyName("ImplementationName")]
    public string ImplementationName { get; set; }

    /// <summary>
    /// Gets or sets the implementation version.
    /// </summary>
    [JsonPropertyName("ImplementationVersion")]
    public string ImplementationVersion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Yggdrasil authentication endpoints accept a
    /// Minecraft username in addition to an email address as the login identifier.
    /// Exposed to clients as <c>meta.feature.non_email_login</c>.
    /// </summary>
    [JsonPropertyName("AllowProfileNameLogin")]
    public bool AllowProfileNameLogin { get; set; }

    /// <summary>
    /// Gets or sets the lifetime of a Yggdrasil access token, in days.
    /// </summary>
    [JsonPropertyName("TokenTtlHours")]
    public int TokenTtlHours { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrently active Yggdrasil access tokens retained per user.
    /// </summary>
    [JsonPropertyName("MaxActiveTokensPerUser")]
    public int MaxActiveTokensPerUser { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the <c>minecraftservices/player/certificates</c>
    /// endpoint is served. Exposed to clients as <c>meta.feature.enable_profile_key</c>.
    /// </summary>
    [JsonPropertyName("EnableProfileKey")]
    public bool EnableProfileKey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether username validation is enforced on registration and
    /// requested from authlib-injector via <c>meta.feature.username_check</c>.
    /// </summary>
    [JsonPropertyName("EnforceUsernameCheck")]
    public bool EnforceUsernameCheck { get; set; }

    /// <summary>
    /// Gets or sets the site homepage URL advertised as <c>meta.links.homepage</c>.
    /// Null when not configured, in which case the link is omitted from the metadata response.
    /// </summary>
    [JsonPropertyName("HomepageUrl")]
    public string? HomepageUrl { get; set; }

    /// <summary>
    /// Gets or sets the registration page URL advertised as <c>meta.links.register</c>.
    /// Null or empty when account registration is disabled, in which case the link is omitted.
    /// </summary>
    [JsonPropertyName("RegisterUrl")]
    public string? RegisterUrl { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="YggdrasilConfig"/> class with explicit values.
    /// </summary>
    /// <param name="enforceIpCheckInJoin">Whether the request IP is compared against the play session IP when joining a server.</param>
    /// <param name="enforceIpCheckInHasJoined">Whether the IP reported by the game server is compared against the stored join IP on hasJoined requests.</param>
    /// <param name="allowEmptyJoinedAddress">Whether hasJoined requests may omit the IP address.</param>
    /// <param name="blockedServers">An array of server identifiers clients are not allowed to join.</param>
    /// <param name="skinDomains">An array of allowed domains for serving skins.</param>
    /// <param name="serverName">The server name presented by Yggdrasil-compatible endpoints.</param>
    /// <param name="implementationName">The name of the Yggdrasil implementation (metadata shown to clients).</param>
    /// <param name="implementationVersion">The version string of the Yggdrasil implementation.</param>
    /// <param name="allowProfileNameLogin">Whether a username is accepted as the login identifier alongside an email address.</param>
    /// <param name="tokenTtlHours">The lifetime of a Yggdrasil access token, in days.</param>
    /// <param name="maxActiveTokensPerUser">The maximum number of active access tokens retained per user.</param>
    /// <param name="enableProfileKey">Whether the minecraftservices player certificates endpoint is served.</param>
    /// <param name="enforceUsernameCheck">Whether username validation is enforced and advertised to authlib-injector.</param>
    /// <param name="homepageUrl">The site homepage URL advertised as meta.links.homepage.</param>
    /// <param name="registerUrl">The registration page URL advertised as meta.links.register.</param>
    /// <param name="enforceAgent">Whether session server requests are validated against the configured agent.</param>
    /// <param name="agent">The user agent accepted by the session server endpoints.</param>
    public YggdrasilConfig(bool enforceIpCheckInJoin, bool enforceIpCheckInHasJoined, bool allowEmptyJoinedAddress, string[] blockedServers, string[] skinDomains, string serverName,
        string implementationName, string implementationVersion, bool allowProfileNameLogin = false, int tokenTtlHours = 30, int maxActiveTokensPerUser = 10,
        bool enableProfileKey = false, bool enforceUsernameCheck = true, string? homepageUrl = null, string? registerUrl = null,
        bool enforceAgent = false, AgentConfig? agent = null)
    {
        EnforceIpCheckInJoin = enforceIpCheckInJoin;
        EnforceIpCheckInHasJoined = enforceIpCheckInHasJoined;
        AllowEmptyJoinedAddress = allowEmptyJoinedAddress;
        BlockedServers = blockedServers;
        SkinDomains = skinDomains;
        ServerName = serverName;
        ImplementationName = implementationName;
        ImplementationVersion = implementationVersion;
        AllowProfileNameLogin = allowProfileNameLogin;
        TokenTtlHours = tokenTtlHours;
        MaxActiveTokensPerUser = maxActiveTokensPerUser;
        EnableProfileKey = enableProfileKey;
        EnforceUsernameCheck = enforceUsernameCheck;
        HomepageUrl = homepageUrl;
        RegisterUrl = registerUrl;
        EnforceAgent = enforceAgent;
        Agent = agent ?? new AgentConfig("Minecraft", 1);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YggdrasilConfig"/> class and loads settings from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration to load settings from.</param>
    /// <exception cref="InvalidOperationException">Thrown if a required Yggdrasil configuration value is missing.</exception>
    public YggdrasilConfig(IConfiguration configuration)
    {
        EnableLegacyAuth = configuration.GetValue(Constants.ConfigurationKeys.YggdrasilEnableLegacyAuth, false);
        EnforceIpCheckInJoin = configuration.GetValue(Constants.ConfigurationKeys.YggdrasilEnforceIpCheckInJoin, true);
        EnforceIpCheckInHasJoined = configuration.GetValue(Constants.ConfigurationKeys.YggdrasilEnforceIpCheckInHasJoined, true);
        AllowEmptyJoinedAddress = configuration.GetValue(Constants.ConfigurationKeys.YggdrasilAllowEmptyJoinedAddress, false);
        BlockedServers = configuration.GetSection(Constants.ConfigurationKeys.YggdrasilBlockedServers).Get<string[]>() ?? [];
        SkinDomains = configuration.GetSection(Constants.ConfigurationKeys.YggdrasilSkinDomains).Get<string[]>() ?? throw new InvalidOperationException(Constants.ConfigurationKeys.YggdrasilSkinDomains);
        ServerName = AppConfiguration.GetString(configuration, Constants.ConfigurationKeys.YggdrasilServerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(ServerName);
        ImplementationName = AppConfiguration.GetString(configuration, Constants.ConfigurationKeys.YggdrasilImplementationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(ImplementationName);
        ImplementationVersion = AppConfiguration.GetString(configuration, Constants.ConfigurationKeys.YggdrasilImplementationVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(ImplementationVersion);
        AllowProfileNameLogin = configuration.GetValue(Constants.ConfigurationKeys.YggdrasilAllowProfileNameLogin, false);
        TokenTtlHours = configuration.GetValue(Constants.ConfigurationKeys.YggdrasilTokenTtlHours, 30);
        MaxActiveTokensPerUser = configuration.GetValue(Constants.ConfigurationKeys.YggdrasilMaxActiveTokensPerUser, 10);
        EnableProfileKey = configuration.GetValue(Constants.ConfigurationKeys.YggdrasilEnableProfileKey, false);
        EnforceUsernameCheck = configuration.GetValue(Constants.ConfigurationKeys.YggdrasilEnforceUsernameCheck, true);
        HomepageUrl = configuration.GetValue<string>(Constants.ConfigurationKeys.YggdrasilHomepageUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(HomepageUrl);
        RegisterUrl = configuration.GetValue<string>(Constants.ConfigurationKeys.YggdrasilRegisterUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(RegisterUrl);
        EnforceAgent = configuration.GetValue(Constants.ConfigurationKeys.YggdrasilEnforceAgent, false);
        Agent = new AgentConfig(configuration);
    }
}
