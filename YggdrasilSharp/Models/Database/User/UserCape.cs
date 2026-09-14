using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Tavstal.YggdrasilSharp.Models.Database.User;

/// <summary>
/// Represents the association between a user and a cape, including metadata
/// such as selection status and timestamps.
/// </summary>
public class UserCape
{
    /// <summary>
    /// Gets or sets the unique identifier for the UserCape record.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }
    
    /// <summary>
    /// Gets or sets the unique identifier of the user associated with the cape.
    /// </summary>
    [StringLength(36)]
    public required string UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the unique identifier of the cape associated with the user.
    /// </summary>
    public required ulong CapeId { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the cape is selected by the user.
    /// </summary>
    public bool IsSelected { get; set; }    
    
    /// <summary>
    /// Gets or sets the reason for the cape's association, if any.
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the UserCape record was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the UserCape record was last updated.
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; set; }
    
    /* ######################################################################
     *                         NAVIGATION PROPERTIES
     * ###################################################################### */
    
    /// <summary>
    /// Gets or sets the user associated with this UserCape record.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [ForeignKey("UserId")] 
    [JsonIgnore]
    public CustomUser? User { get; set; } 
    
    /// <summary>
    /// Gets or sets the cape associated with this UserCape record.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [ForeignKey("CapeId")] 
    [JsonIgnore]
    public Cape? Cape { get; set; }
}
