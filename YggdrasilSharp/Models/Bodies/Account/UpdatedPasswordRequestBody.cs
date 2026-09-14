using System.ComponentModel.DataAnnotations;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Account;

/// <summary>
/// Represents the request body for updating a user's password.
/// </summary>
public class UpdatedPasswordRequestBody
{
    /// <summary>
    /// Gets or sets the current password of the user.
    /// </summary>
    [Required]
    public required string CurrentPassword { get; set; }
    
    /// <summary>
    /// Gets or sets the new password to be set for the user.
    /// </summary>
    [Required]
    public required string NewPassword { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether to log out the user from all sessions.
    /// Defaults to false.
    /// </summary>
    public bool LogoutEverywhere { get; set; } = false;
}