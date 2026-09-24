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
    public Dictionary<string, FixedWindowRule> FixedWindowRules { get; set; } = [];
    
    /// <summary>
    /// Gets or sets the sliding window rules, keyed by rate limit category.
    /// </summary>
    public Dictionary<string, SlidingWindowRule> SlidingWindowRules { get; set; } = [];
    
    /// <summary>
    /// Gets or sets the concurrent rules, keyed by rate limit category.
    /// </summary>
    public Dictionary<string, ConcurrentRule> ConcurrentRules { get; set; } = [];
    
    /// <summary>
    /// Gets or sets the token bucket rules, keyed by rate limit category.
    /// </summary>
    public Dictionary<string, TokenBucketRule> TokenBucketRules { get; set; } = [];
}