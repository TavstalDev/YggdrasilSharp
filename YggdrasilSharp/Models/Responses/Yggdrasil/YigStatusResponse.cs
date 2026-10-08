using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Responses.Yggdrasil;

/// <summary>
/// Represents the response body returned by the Yggdrasil root status endpoint.
/// </summary>
public class YigStatusResponse
{
    /// <summary>
    /// Gets or sets the allow-listed domains from which skins and capes may be loaded.
    /// </summary>
    [JsonPropertyName("skinDomains")]
    public required string[] SkinDomains { get; set; }

    /// <summary>
    /// Gets or sets the PEM-encoded RSA public key used to verify signed profile properties.
    /// </summary>
    [JsonPropertyName("signaturePublickey")]
    public required string SignaturePublicKey { get; set; }

    /// <summary>
    /// Gets or sets the metadata describing the server and the features it supports.
    /// </summary>
    [JsonPropertyName("meta")]
    public required Dictionary<string, object> Meta { get; set; }
}
