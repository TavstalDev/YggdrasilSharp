using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Auth;

/// <summary>
/// Represents the request body for logging into the launcher.
/// </summary>
public class LauncherLoginRequestBody
{
    /// <summary>
    /// Gets or initializes the username of the user.
    /// </summary>
    [Required]
    [StringLength(16, MinimumLength = 3)]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public required string Username { get; init; }

    /// <summary>
    /// Gets or initializes the password of the user.
    /// </summary>
    [Required]
    [StringLength(64, MinimumLength = 8)]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public required string Password { get; init; }
}
