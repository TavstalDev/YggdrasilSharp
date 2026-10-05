using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Tavstal.YggdrasilSharp.Models.Database.User;

/// <summary>
/// Represents a custom user token that extends the IdentityUserToken class.
/// Includes additional properties such as a unique identifier and creation date.
/// </summary>
public sealed class CustomUserToken : IdentityUserToken<string>
{
    /// <summary>
    /// Gets or sets the unique identifier for the user token.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the user ID associated with the token.
    /// </summary>
    [StringLength(36)]
    public override string UserId { get; set; }

    /// <summary>
    /// Gets or sets the name of the token.
    /// </summary>
    [StringLength(32)]
    public override string Name { get; set; }

    /// <summary>
    /// Gets or sets the value of the token.
    /// </summary>
    [StringLength(256)]
    [Required]
    public override string? Value { get; set; }

    /// <summary>
    /// Gets or sets the login provider associated with the token.
    /// </summary>
    [StringLength(32)]
    public override string LoginProvider { get; set; }

    /// <summary>
    /// Gets or sets the creation date of the token.
    /// </summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomUserToken"/> class.
    /// </summary>
    public CustomUserToken() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomUserToken"/> class with the specified properties.
    /// </summary>
    /// <param name="userId">The user ID associated with the token.</param>
    /// <param name="name">The name of the token.</param>
    /// <param name="value">The value of the token.</param>
    /// <param name="loginProvider">The login provider associated with the token.</param>
    /// <param name="createDate">The creation date of the token.</param>
    public CustomUserToken(string userId, string name, string value, string loginProvider, DateTimeOffset createDate)
    {
        UserId = userId;
        Name = name;
        Value = value;
        LoginProvider = loginProvider;
        CreateDate = createDate;
    }

    /* ######################################################################
     *                         NAVIGATION PROPERTIES
     * ###################################################################### */

    /// <summary>
    /// Gets or sets the user associated with the token.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [ForeignKey("UserId")]
    [JsonIgnore]
    public CustomUser? User { get; set; }
}
