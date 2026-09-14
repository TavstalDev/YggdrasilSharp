using Tavstal.YggdrasilSharp.Models.Database.User;

namespace Tavstal.YggdrasilSharp.Models.Database;

/// <summary>
/// Represents the result of an authentication / sign-in operation.
/// </summary>
public class SignInResult
{
   /// <summary>
    /// True when the sign-in flow completed successfully and the user is authenticated.
    /// </summary>
    public bool Succeeded { get; set; }
    
    /// <summary>
    /// True when the sign-in requires a second factor (e.g., OTP, authenticator app).
    /// </summary>
    public bool RequiresTwoFactor { get; set; }
    
    /// <summary>
    /// Optional human-readable message describing the sign-in result or failure reason.
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// A session-level token issued as part of a twofactor authentication flow.
    /// </summary>
    public string? SessionToken { get; set; }
    
    /// <summary>
    /// The UTC date/time when the <see cref="SessionToken"/> (or any other temporary token represented
    /// in this result) expires. If null, no explicit expiry is provided with this result.
    /// </summary>
    public DateTimeOffset? TokenExpiresAt { get; set; }
    
    /// <summary>
    /// The authenticated user details when available. Null if authentication did not complete.
    /// </summary>
    /// <seealso cref="CustomUserLogin"/>
    /// <seealso cref="CustomUserToken"/>
    public CustomUser? User { get; set; }
    
    /// <summary>
    /// The user's login record (e.g., provider, provider key) related to this sign-in attempt.
    /// </summary>
    public CustomUserLogin? UserLogin { get; set; }
    
    /// <summary>
    /// An authentication token or token record associated with the user session.
    /// </summary>
    public CustomUserToken? UserToken { get; set; }

    /// <summary>
    /// Returns <c>true</c> when the result includes a fully populated user identity set:
    /// <see cref="User"/>, <see cref="UserLogin"/>, and <see cref="UserToken"/> are all non-null.
    /// </summary>
    /// <returns>True if the three user-related properties are populated, otherwise false.</returns>
    public bool HasValidUser()
    {
        return User != null && UserLogin != null && UserToken != null;
    }
}