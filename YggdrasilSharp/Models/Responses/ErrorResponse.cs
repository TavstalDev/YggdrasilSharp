using System.Net;
using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Responses;

/// <summary>
/// Represents the response body returned when a request could not be fulfilled.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Gets or sets the status code that describes the outcome of the failed request.
    /// </summary>
    [JsonPropertyName("StatusCode")]
    public HttpStatusCode StatusCode { get; set; }
    
    /// <summary>
    /// Gets or sets the message describing why the request failed.
    /// </summary>
    [JsonPropertyName("Message")]
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets additional information about the failure.
    /// </summary>
    [JsonPropertyName("Details")]
    public string? Details { get; set; }
}