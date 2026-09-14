namespace Tavstal.YggdrasilSharp.Models.Database.User.Claims;

/// <summary>
/// Represents a custom claim with a specific type and value.
/// </summary>
public class CustomClaim
{
    /// <summary>
    /// Gets or sets the type of the claim.
    /// </summary>
    public string Type { get; set; }
    /// <summary>
    /// Gets or sets the value of the claim.
    /// </summary>
    public string Value { get; set; }
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomClaim"/> class with the specified type and value.
    /// </summary>
    /// <param name="type">The type of the claim.</param>
    /// <param name="value">The value of the claim.</param>
    public CustomClaim(string type, string value)
    {
        Type = type;
        Value = value;
    }
}
