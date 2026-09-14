namespace Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;

/// <summary>
/// Represents the request body for validating an access token in the Yggdrasil API.
/// </summary>
public class YigValidateRequest
{
    /// <summary>
    /// Gets or sets the access token to be validated.
    /// This field is required.
    /// </summary>
    public required string accessToken { get; set; }

    /// <summary>
    /// Gets or sets the client token associated with the session.
    /// This field is optional.
    /// </summary>
    public string? clientToken { get; set; }
}
