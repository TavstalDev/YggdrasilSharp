#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Tavstal.YggdrasilSharp.Models.Bodies.Auth;

/// <summary>
/// Represents the response body for a login operation.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// Gets or sets the message associated with the login response.
    /// This could be a success or error message.
    /// </summary>
    public string Message { get; set; } 
    
    /// <summary>
    /// Gets or sets the unique identifier of the user.
    /// This identifies the user who logged in.
    /// </summary>
    public ulong UserId { get; set; } 
    
    /// <summary>
    /// Gets or sets the authentication token issued upon successful login.
    /// This token is used for subsequent authenticated requests.
    /// </summary>
    public string Token { get; set; } 
    
    /// <summary>
    /// Gets or sets the expiration time of the authentication token.
    /// This indicates when the token will expire.
    /// </summary>
    public string Expires { get; set; } 
}
