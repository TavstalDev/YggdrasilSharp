namespace Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;

/// <summary>
/// Represents the request body for signing out in the Yggdrasil API.
/// </summary>
public class YigSignoutRequest
{
    /// <summary>
    /// Gets or sets the username of the user.
    /// This field is required.
    /// </summary>
    public required string username { get; set; }

    /// <summary>
    /// Gets or sets the password of the user.
    /// This field is required.
    /// </summary>
    public required string password { get; set; }
}
