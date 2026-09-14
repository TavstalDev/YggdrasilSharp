namespace Tavstal.YggdrasilSharp.Models;

/// <summary>
/// A static class that defines constant strings representing various rate limit categories.
/// These categories can be used to identify and enforce rate limiting policies in the application.
/// </summary>
/// <remarks>
/// <para>
/// <strong>IMPORTANT:</strong> This class serves as a helper for rate limit category identifiers only.
/// The actual rate limit configuration (PermitLimit, Window duration, QueueLimit) is defined in 
/// <c>appsettings.json</c> under the "RateLimiting" section.
/// </para>
/// <para>
/// The constants defined here should match the configuration keys in appsettings.json.
/// Update both this class and appsettings.json when adding or modifying rate limit policies.
/// </para>
/// </remarks>
public static class RateLimits
{
    /// <summary>
    /// Represents the default rate limit category.
    /// </summary>
    public const string DEFAULT = "Default";

    /// <summary>
    /// Represents the rate limit category for user registration actions.
    /// </summary>
    public const string AUTH_REGISTER = "AuthRegister";
    /// <summary>
    /// Represents the rate limit category for user login actions.
    /// </summary>
    public const string AUTH_LOGIN = "AuthLogin";

    /// <summary>
    /// Represents the rate limit category for password reset actions.
    /// </summary>
    public const string AUTH_RESET = "AuthReset";

    /// <summary>
    /// Represents the rate limit category for file upload actions.
    /// </summary>
    public const string UPLOAD = "Upload";

    /// <summary>
    /// Represents the rate limit category for file download actions.
    /// </summary>
    public const string DOWNLOAD = "Download";

    /// <summary>
    /// Represents the rate limit category for search actions.
    /// </summary>
    public const string SEARCH = "Search";

    /// <summary>
    /// Represents the rate limit category for write actions.
    /// </summary>
    public const string WRITE = "Write";

    /// <summary>
    /// Represents the rate limit category for administrative actions.
    /// </summary>
    public const string ADMIN = "Admin";
}
