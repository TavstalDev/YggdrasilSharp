using System.ComponentModel.DataAnnotations;

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
    [MinLength(3)]
    [MaxLength(16)]
    public required string Username { get; init; }
    
    /// <summary>
    /// Gets or initializes the password of the user.
    /// </summary>
    [Required]
    [MinLength(8)]
    [MaxLength(64)]
    public required string Password { get; init; }
}
