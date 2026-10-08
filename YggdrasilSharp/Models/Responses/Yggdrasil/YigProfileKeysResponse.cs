using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Responses.Yggdrasil;

/// <summary>
/// Represents the response body returned by the profile keys endpoint,
/// containing the public keys used to verify signed profile properties.
/// </summary>
public class YigProfileKeysResponse
{
    /// <summary>
    /// Gets or sets the public keys used to verify signed profile properties.
    /// </summary>
    [JsonPropertyName("profileKeys")]
    public required string[] ProfileKeys { get; set; } = [];
}
