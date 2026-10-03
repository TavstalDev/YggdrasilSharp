using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Tavstal.YggdrasilSharp.Models.Database.User;

/// <summary>
/// Represents a custom user role that extends the IdentityUserRole class.
/// Includes navigation properties to the associated user and role.
/// </summary>
public class CustomUserRole : IdentityUserRole<string>
{
    /// <summary>
    /// Gets or sets the unique identifier for the user associated with this role.
    /// </summary>
    [StringLength(36)]
    public override string UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the unique identifier for the role associated with this user.
    /// </summary>
    [StringLength(36)]
    public override string RoleId { get; set; }
    
    /* ######################################################################
     *                         NAVIGATION PROPERTIES
     * ###################################################################### */
    
    /// <summary>
    /// Gets or sets the user associated with this role.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [ForeignKey("UserId")]
    [JsonIgnore]
    public CustomUser? User { get; set; }
    
    /// <summary>
    /// Gets or sets the role associated with this user.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [ForeignKey("RoleId")]
    [JsonIgnore]
    public CustomRole? Role { get; set; }
}
