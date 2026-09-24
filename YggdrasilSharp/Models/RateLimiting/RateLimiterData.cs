using System.Text.Json.Serialization;
using Tavstal.YggdrasilSharp.Models.RateLimiting.Rules;

namespace Tavstal.YggdrasilSharp.Models.RateLimiting;

/// <summary>
/// Holds the configured rules for each rate limit category.
/// Each rule is stored with the category name as the key.
/// </summary>
public class RateLimiterData
{
    /// <summary>
    /// Gets or sets the fixed window rules, keyed by rate limit category.
    /// </summary>
    [JsonPropertyName("FixedWindow")]
    public Dictionary<string, FixedWindowRule> FixedWindow { get; set; } = [];
    
    /// <summary>
    /// Gets or sets the sliding window rules, keyed by rate limit category.
    /// </summary>
    [JsonPropertyName("SlidingWindow")]
    public Dictionary<string, SlidingWindowRule> SlidingWindow { get; set; } = [];
    
    /// <summary>
    /// Gets or sets the concurrent rules, keyed by rate limit category.
    /// </summary>
    [JsonPropertyName("Concurrent")]
    public Dictionary<string, ConcurrentRule> Concurrent { get; set; } = [];
    
    /// <summary>
    /// Gets or sets the token bucket rules, keyed by rate limit category.
    /// </summary>
    [JsonPropertyName("TokenBucket")]
    public Dictionary<string, TokenBucketRule> TokenBucket { get; set; } = [];
}