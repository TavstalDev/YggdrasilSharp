namespace Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;

/// <summary>
/// Represents the request body for refreshing an access token in the Yggdrasil API.
/// </summary>
public class YigRefreshRequest
{
    /// <summary>
    /// Gets or sets the access token to be refreshed.
    /// This field is required.
    /// </summary>
    public required string accessToken { get; set; }

    /// <summary>
    /// Gets or sets the client token associated with the session.
    /// This field is optional.
    /// </summary>
    public string? clientToken { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include user information in the response.
    /// </summary>
    public bool requestUser { get; set; }

    /// <summary>
    /// Gets or sets the selected profile for the request.
    /// </summary>
    public required YigProfileBody selectedProfile { get; set; }
}
