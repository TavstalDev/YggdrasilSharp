namespace Tavstal.YggdrasilSharp.Models.Config;

/// <summary>
/// Represents the Yggdrasil-compatible server settings loaded from the configuration.
/// </summary>
public class YggdrasilConfig
{
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
    /// <param name="skinDomains">An array of allowed domains for serving skins.</param>
    /// <param name="serverName">The server name presented by Yggdrasil-compatible endpoints.</param>
    /// <param name="implementationName">The name of the Yggdrasil implementation (metadata shown to clients).</param>
    /// <param name="implementationVersion">The version string of the Yggdrasil implementation.</param>
    public YggdrasilConfig(string[] skinDomains, string serverName, string implementationName, string implementationVersion)
    {
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
        SkinDomains = configuration.GetSection(Constants.ConfigurationKeys.YggdrasilSkinDomains).Get<string[]>() ?? throw new InvalidOperationException(Constants.ConfigurationKeys.YggdrasilSkinDomains);
        ServerName = Settings.GetString(configuration, Constants.ConfigurationKeys.YggdrasilServerName);
        ImplementationName = Settings.GetString(configuration, Constants.ConfigurationKeys.YggdrasilImplementationName);
        ImplementationVersion = Settings.GetString(configuration, Constants.ConfigurationKeys.YggdrasilImplementationVersion);
    }
}