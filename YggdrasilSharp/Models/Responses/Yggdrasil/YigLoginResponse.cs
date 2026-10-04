using System.Text.Json.Serialization;
using Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;

namespace Tavstal.YggdrasilSharp.Models.Responses.Yggdrasil;

/// <summary>
/// Represents the response body returned by a successful Yggdrasil authentication request.
/// </summary>
public class YigLoginResponse
{
    /// <summary>
    /// Gets or sets the access token issued for the session.
    /// </summary>
    [JsonPropertyName("accessToken")]
    public required string AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the client token bound to the session.
    /// </summary>
    [JsonPropertyName("clientToken")]
    public required string ClientToken { get; set; }

    /// <summary>
    /// Gets or sets the profiles the account is allowed to play with.
    /// </summary>
    [JsonPropertyName("availableProfiles")]
    public required List<YigProfileBody> AvailableProfiles { get; set; }

    /// <summary>
    /// Gets or sets the profile selected for the session.
    /// </summary>
    [JsonPropertyName("selectedProfile")]
    public required YigProfileBody SelectedProfile { get; set; }

    /// <summary>
    /// Gets or sets the user the session was issued for. Optional; clients that do not request
    /// user details receive a response without this field.
    /// </summary>
    [JsonPropertyName("user")]
    public YigUser? User { get; set; }
}
