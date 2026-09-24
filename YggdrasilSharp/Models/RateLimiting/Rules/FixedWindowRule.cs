using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
// ReSharper disable ClassNeverInstantiated.Global

namespace Tavstal.YggdrasilSharp.Models.RateLimiting.Rules;

/// <summary>
/// Options that control the behavior of a fixed window rate limiter.
/// </summary>
public class FixedWindowRule
{
    /// <summary>
    /// Gets or sets the maximum number of requests allowed in a window.
    /// </summary>
    [JsonPropertyName("PermitLimit")]
    public int PermitLimit { get; set; }
    
    /// <summary>
    /// Gets or sets the length of the window in seconds.
    /// </summary>
    [JsonPropertyName("WindowSeconds")]
    public int WindowSeconds { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum number of queued requests.
    /// </summary>
    [JsonPropertyName("QueueLimit")]
    public int QueueLimit { get; set; }
    
    /// <summary>
    /// Gets or sets how queued requests are ordered when the limit is reached.
    /// </summary>
    [JsonPropertyName("ProcessingOrder")]
    public QueueProcessingOrder ProcessingOrder { get; set; }
}