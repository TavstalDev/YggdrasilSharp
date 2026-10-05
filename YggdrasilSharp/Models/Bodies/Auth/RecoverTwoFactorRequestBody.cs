using System.ComponentModel.DataAnnotations;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Auth;

/// <summary>
/// Represents the request body for recovering a two-factor authentication session.
/// </summary>
public class RecoverTwoFactorRequestBody
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
    /// Gets or sets the recovery token that was issued to the user (for example via email).
    /// This token is required and is validated to ensure the recovery request is authorized.
    /// </summary>
    [Required]
    [StringLength(48)]
    public required string RecoveryToken { get; set; }

    /// <summary>
    /// Gets or sets the backup code used to recover the two-factor authentication session.
    /// This field is required.
    /// </summary>
    [Required]
    [StringLength(6)]
    public required string BackupCode { get; set; }
}
