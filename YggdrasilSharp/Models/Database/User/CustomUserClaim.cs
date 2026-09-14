using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace Tavstal.YggdrasilSharp.Models.Database.User;

/// <summary>
/// Represents a custom user claim that extends the IdentityUserClaim class.
/// Includes a navigation property to the associated user.
/// </summary>
public class CustomUserClaim : IdentityUserClaim<string>
{
    /* ######################################################################
     *                         NAVIGATION PROPERTIES
     * ###################################################################### */
    
    /// <summary>
    /// Gets or sets the user associated with this claim.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [ForeignKey("UserId")]
    [JsonIgnore]
    public CustomUser? User { get; set; }
}
