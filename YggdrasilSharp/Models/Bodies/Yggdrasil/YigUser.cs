using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil
;

/// <summary>
/// Represents the user attached to a Yggdrasil session, as returned by the
/// <c>authenticate</c> and <c>refresh</c> endpoints.
/// </summary>
public class YigUser
{
    /// <summary>
    /// Gets or sets the unique identifier of the user, exposed to the client as <c>id</c>.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the username of the user the session was issued for, exposed to the client as <c>username</c>.
    /// </summary>
    [JsonPropertyName("username")]
    public required  string Username { get; set; }

    /// <summary>
    /// Gets or sets the user properties, such as <c>preferred_language</c>, exposed to the client as <c>properties</c>.
    /// </summary>
    [JsonPropertyName("properties")]
    public List<YigUserProperty> Properties { get; set; } = [];
}
