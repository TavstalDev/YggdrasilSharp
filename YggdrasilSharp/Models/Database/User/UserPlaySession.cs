using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Tavstal.YggdrasilSharp.Models.Database.User;

/// <summary>
/// Represents a user's play session, including session details such as token,
/// IP address, and timestamps for creation and expiration.
/// </summary>
public class UserPlaySession
{
    /// <summary>
    /// Gets or sets the unique identifier for the play session.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }
    
    /// <summary>
    /// Gets or sets the unique identifier of the user associated with the play session.
    /// </summary>
    [StringLength(36)]
    public required string UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the token associated with the play session.
    /// </summary>
    [StringLength(255)]
    public required string Token { get; set; }
    
    /// <summary>
    /// Gets or sets the IP address of the user during the play session.
    /// </summary>
    [StringLength(50)]
    public required string UserIp { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the play session was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the play session expires.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; set; }
    
    /* ######################################################################
     *                         NAVIGATION PROPERTIES
     * ###################################################################### */
    
    /// <summary>
    /// Gets or sets the user associated with the play session.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [ForeignKey("UserId")]
    [JsonIgnore]
    public CustomUser? User { get; set; }
}
