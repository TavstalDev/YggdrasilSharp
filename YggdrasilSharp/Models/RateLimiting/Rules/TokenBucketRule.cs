using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
// ReSharper disable ClassNeverInstantiated.Global

namespace Tavstal.YggdrasilSharp.Models.RateLimiting.Rules;

/// <summary>
/// Options that control the behavior of a token bucket rate limiter.
/// </summary>
public class TokenBucketRule
{
    /// <summary>
    /// Gets or sets the maximum number of queued requests.
    /// </summary>
    [JsonPropertyName("QueueLimit")]
    public int QueueLimit { get; set; }
    
    /// <summary>
    /// Gets or sets how often new tokens are added, in seconds.
    /// </summary>
    [JsonPropertyName("ReplenishmentSeconds")]
    public int ReplenishmentSeconds { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum number of tokens the bucket can hold.
    /// </summary>
    [JsonPropertyName("TokenLimit")]
    public int TokenLimit { get; set; }
    
    /// <summary>
    /// Gets or sets the number of tokens added in each replenishment.
    /// </summary>
    [JsonPropertyName("TokensPerPeriod")]
    public int TokensPerPeriod { get; set; }
    
    /// <summary>
    /// Gets or sets how queued requests are ordered when the limit is reached.
    /// </summary>
    [JsonPropertyName("ProcessingOrder")]
    public QueueProcessingOrder ProcessingOrder { get; set; }
}