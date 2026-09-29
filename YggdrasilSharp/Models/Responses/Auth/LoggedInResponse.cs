using Tavstal.YggdrasilSharp.Models.Database.User;
using Tavstal.YggdrasilSharp.Models.Database.User.Claims;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Tavstal.YggdrasilSharp.Models.Responses.Auth;

/// <summary>
/// Represents the response body for a logged-in user.
/// </summary>
public class LoggedInResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the user.
    /// </summary>
    public ulong UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the username of the user.
    /// </summary>
    public string Username { get; set; }
    
    /// <summary>
    /// Gets or sets the display name of the user.
    /// </summary>
    public string DisplayName { get; set; }
    
    /// <summary>
    /// Gets or sets the email address of the user.
    /// </summary>
    public string Email { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the user has an avatar.
    /// </summary>
    public bool HasAvatar { get; set; }
    
    /// <summary>
    /// Gets or sets the URL or path to the user's avatar.
    /// </summary>
    public string Avatar { get; set; }
    
    /// <summary>
    /// Gets or sets the list of roles assigned to the user.
    /// </summary>
    public List<CustomRole> Roles { get; set; }
    
    /// <summary>
    /// Gets or sets the list of claims associated with the user.
    /// </summary>
    public List<CustomClaim> Claims { get; set; }
}
