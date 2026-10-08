using System.ComponentModel.DataAnnotations;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Auth;

/// <summary>
/// Represents the request body for confirming a user registration.
/// </summary>
public class ConfirmRegisterRequestBody
{
    /// <summary>
    /// Gets or sets the unique identifier of the user.
    /// </summary>
    [Required]
    [StringLength(36, MinimumLength = 32)]
    public required string UserId { get; set; }

    /// <summary>
    /// Gets or sets the confirmation token for verifying the registration.
    /// </summary>
    [Required]
    [StringLength(64)]
    public required string ConfirmationToken { get; set; }
}
