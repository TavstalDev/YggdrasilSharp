namespace Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;

/// <summary>
/// Represents the request body for invalidating an access token in the Yggdrasil API.
/// </summary>
public class YigInvalidateRequest
{
    /// <summary>
    /// Gets or sets the access token to be invalidated.
    /// This field is required.
    /// </summary>
    public required String accessToken { get; set; }
}
