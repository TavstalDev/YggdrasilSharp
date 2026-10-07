using System.ComponentModel.DataAnnotations;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Auth;

/// <summary>
/// Represents the request body for logging into the system with a two-factor authentication session.
/// </summary>
public class LoginTFASessionRequestBody
{
    /// <summary>
    /// Gets or initializes the session token returned by the initial login redirect.
    /// This token is required to authenticate the two-factor session.
    /// </summary>
    [Required]
    [StringLength(255)]
    public required string SessionToken { get; init; }

    /// <summary>
    /// Gets or initializes the identifier of the user associated with the two-factor session.
    /// </summary>
    [Required]
    [StringLength(36)]
    public required string UserId { get; init; }

    /// <summary>
    /// Gets or initializes the two-factor authentication code.
    /// This code is required to complete the login process.
    /// </summary>
    [Required]
    [StringLength(6)]
    public required string TwoFactorCode { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the user should remain logged in.
    /// This field is optional and defaults to false.
    /// </summary>
    public bool RememberMe { get; set; } = false;
}
