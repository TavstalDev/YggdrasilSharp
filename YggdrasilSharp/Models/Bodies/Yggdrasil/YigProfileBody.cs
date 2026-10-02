using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;

/// <summary>
/// Represents the profile body in the Yggdrasil API.
/// </summary>
public class YigProfileBody
{
    /// <summary>
    /// Gets or sets the unique identifier of the profile.
    /// This field is required.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the profile.
    /// This field is required.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}
