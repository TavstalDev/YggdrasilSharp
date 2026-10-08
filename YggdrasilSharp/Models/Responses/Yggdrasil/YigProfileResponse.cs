using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Responses.Yggdrasil;

/// <summary>
/// Represents a player profile returned by the profile lookup endpoints.
/// </summary>
public class YigProfileResponse
{
    /// <summary>
    /// Gets or sets the unique ID of the profile.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the player name of the profile.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}
