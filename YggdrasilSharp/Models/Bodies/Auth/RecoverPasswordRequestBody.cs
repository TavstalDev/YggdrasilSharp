using System.ComponentModel.DataAnnotations;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Auth;

/// <summary>
/// Represents the request body for recovering a user's password.
/// </summary>
public class RecoverPasswordRequestBody
{
    /// <summary>
    /// Gets or sets the email address associated with the user's account.
    /// This field is required.
    /// </summary>
    [Required]
    [EmailAddress]
    [MinLength(3)]
    [MaxLength(254)]
    public required string Email { get; set; }
    
    /// <summary>
    /// Gets or sets the recovery token used to verify the password recovery request.
    /// This field is required.
    /// </summary>
    [Required]
    [StringLength(48)]
    public required string RecoveryToken { get; set; }
    
    /// <summary>
    /// Gets or sets the new password for the user's account.
    /// This field is required.
    /// </summary>
    [Required]
    [MinLength(8)]
    [MaxLength(64)]
    public required string NewPassword { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether to log the user out of all active sessions.
    /// This field is optional and defaults to false.
    /// </summary>
    public bool LogoutEverywhere { get; set; } = false;
}
