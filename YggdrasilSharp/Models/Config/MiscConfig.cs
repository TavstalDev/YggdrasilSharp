namespace Tavstal.YggdrasilSharp.Models.Config;

/// <summary>
/// Represents miscellaneous runtime settings loaded from the configuration.
/// </summary>
public class MiscConfig
{
    /// <summary>
    /// Gets or sets the website URL.
    /// </summary>
    public string WebsiteUrl { get; set; }

    /// <summary>
    /// Gets or sets the API URL.
    /// </summary>
    public string ApiUrl { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MiscConfig"/> class with explicit values.
    /// </summary>
    /// <param name="websiteUrl">The public website URL (e.g., "https://example.com").</param>
    /// <param name="apiUrl">The base API URL used by clients to access the API (e.g., "https://api.example.com").</param>
    public MiscConfig(string websiteUrl, string apiUrl)
    {
        WebsiteUrl = websiteUrl;
        ApiUrl = apiUrl;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MiscConfig"/> class and loads settings from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration to load settings from.</param>
    /// <exception cref="InvalidOperationException">Thrown if a required runtime configuration value is missing.</exception>
    public MiscConfig(IConfiguration configuration)
    {
        WebsiteUrl = Settings.GetString(configuration, Constants.ConfigurationKeys.RuntimeWebsiteUrl); 
        ApiUrl = Settings.GetString(configuration, Constants.ConfigurationKeys.RuntimeApiUrl);
    }
}