using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;

/// <summary>
/// Represents the client agent information sent to the Yggdrasil API.
/// </summary>
public class YigAgent
{
    /// <summary>
    /// Gets or sets the name of the client.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Minecraft";

    /// <summary>
    /// Gets or sets the version of the client.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;
}
