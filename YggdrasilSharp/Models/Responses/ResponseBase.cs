using System.Net;
using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Responses;

public abstract class ResponseBase
{
    [JsonPropertyName("StatusCode")]
    public HttpStatusCode StatusCode { get; set; }
    
    [JsonPropertyName("Message")]
    public string Message { get; set; } = string.Empty;
}