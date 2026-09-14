using System.ComponentModel.DataAnnotations;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Auth;

/// <summary>
/// Represents the request body for logging into the launcher with a two-factor authentication session.
/// </summary>
public class LauncherLoginTFASessionRequestBody
{
    /// <summary>
    /// Gets or sets the identifier of the user associated with the launcher TFA session.
    /// </summary>
    [Required]
    [MinLength(32)]
    [MaxLength(36)]
    public required string UserId { get; set; }
    
    /// <summary>
    /// Gets or initializes the session token for the login session.
    /// This token is required to authenticate the session.
    /// </summary>
    [Required]
    [StringLength(48)]
    public required string SessionToken { get; init; }
    
    /// <summary>
    /// Gets or initializes the two-factor authentication code.
    /// This code is required to complete the login process.
    /// </summary>
    [Required]
    [StringLength(6)]
    public required string TwoFactorCode { get; init; }
}
