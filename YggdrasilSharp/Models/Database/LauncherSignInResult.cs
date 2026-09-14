using Tavstal.YggdrasilSharp.Models.Database.User;

namespace Tavstal.YggdrasilSharp.Models.Database;

/// <summary>
/// Represents the result of an authentication / sign-in operation.
/// </summary>
public class LauncherSignInResult
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
    /// Optional ephemeral play session information allocated to the user for the launcher.
    /// </summary>
    public UserPlaySession? UserPlaySession { get; set; }
}