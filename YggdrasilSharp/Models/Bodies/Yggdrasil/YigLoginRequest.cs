namespace Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;

/// <summary>
/// Represents the request body for logging in to the Yggdrasil API.
/// </summary>
public class YigLoginRequest
{
    /// <summary>
    /// Gets or sets the username of the user.
    /// This field is required.
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// Gets or sets the password of the user.
    /// This field is required.
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// Gets or sets the client token for the session.
    /// This field is optional.
    /// </summary>
    public string? ClientToken { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include user information in the response.
    /// </summary>
    public bool RequestUser { get; set; }
}
