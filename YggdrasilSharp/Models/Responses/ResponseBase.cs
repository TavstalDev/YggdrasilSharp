using System.Net;
using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Responses;

/// <summary>
/// Base class of the response bodies returned by the authentication and launcher endpoints.
/// </summary>
public abstract class ResponseBase
{
    /// <summary>
    /// Gets or sets the status code that describes the outcome of the request.
    /// </summary>
    [JsonPropertyName("StatusCode")]
    public HttpStatusCode StatusCode { get; set; }
    
    /// <summary>
    /// Gets or sets the message describing the outcome of the request.
    /// </summary>
    [JsonPropertyName("Message")]
    public string Message { get; set; } = string.Empty;
}
