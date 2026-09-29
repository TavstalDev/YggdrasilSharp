using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Responses.Yggdrasil;

/// <summary>
/// Represents the response body returned when a Yggdrasil API request could not be fulfilled.
/// </summary>
public class YigErrorResponse
{
    /// <summary>
    /// Gets or sets the status code describing the outcome of the failed request.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
    
    /// <summary>
    /// Gets or sets additional information about the failure.
    /// </summary>
    [JsonPropertyName("cause")]
    public string? Cause { get; set; }
    
    /// <summary>
    /// Gets or sets the message describing why the request failed.
    /// </summary>
    [JsonPropertyName("ErrorMessage")]
    public string? ErrorMessage { get; set; }
}