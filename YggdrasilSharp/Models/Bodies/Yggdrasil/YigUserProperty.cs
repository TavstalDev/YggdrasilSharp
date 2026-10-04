using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;

/// <summary>
/// Represents a single name/value pair attached to a <see cref="YigUser"/>.
/// </summary>
public class YigUserProperty
{
    /// <summary>
    /// Gets or sets the name of the property, for example <c>preferred_language</c>.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the value of the property.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; }
}
