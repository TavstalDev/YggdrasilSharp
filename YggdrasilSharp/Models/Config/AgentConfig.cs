using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Config;

/// <summary>
/// Represents the user agent accepted by the Yggdrasil session server endpoints.
/// </summary>
public class AgentConfig
{
    /// <summary>
    /// Gets or sets the agent name, the part before the slash in the <c>User-Agent</c> header.
    /// Example: "Minecraft"
    /// </summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; } = "Minecraft";

    /// <summary>
    /// Gets or sets the agent version, the part after the slash in the <c>User-Agent</c> header.
    /// Example: 1
    /// </summary>
    [JsonPropertyName("Version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentConfig"/> class.
    /// </summary>
    public AgentConfig() {}

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentConfig"/> class with explicit values.
    /// </summary>
    /// <param name="name">The agent name presented to clients.</param>
    /// <param name="version">The agent version presented to clients.</param>
    public AgentConfig(string name, int version)
    {
        Name = name;
        Version = version;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentConfig"/> class and loads settings from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration to load settings from.</param>
    public AgentConfig(IConfiguration configuration)
    {
        Name = configuration.GetValue(Constants.ConfigurationKeys.YggdrasilAgentName, "Minecraft");
        Version = configuration.GetValue(Constants.ConfigurationKeys.YggdrasilAgentVersion, 1);
    }
}
