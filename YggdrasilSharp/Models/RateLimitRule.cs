namespace Tavstal.YggdrasilSharp.Models;

/// <summary>
/// Defines a rate limiting rule used to control request flow and prevent abuse.
/// </summary>
public class RateLimitRule
{
    /// <summary>
    /// Gets or sets the maximum number of requests allowed within the time window.
    /// </summary>
    public int PermitLimit { get; set; }

    /// <summary>
    /// Gets or sets the duration of the rate limiting window in seconds.
    /// </summary>
    public int WindowSeconds { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of requests that can be queued when the permit limit is exceeded.
    /// </summary>
    public int QueueLimit { get; set; }
}