using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Tavstal.YggdrasilSharp.Models.Database.User;

namespace Tavstal.YggdrasilSharp.Models.Database.Server;

/// <summary>
/// Represents a record of a user joining a server, including details such as user ID, IP address, 
/// server ID, and timestamps for creation and expiration.
/// </summary>
public class ServerJoin
{
    /// <summary>
    /// Gets or sets the unique identifier for the server join record.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }
    
    /// <summary>
    /// Gets or sets the unique identifier of the user who joined the server.
    /// </summary>
    [StringLength(36)]
    public required string UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the IP address of the user who joined the server.
    /// </summary>
    [StringLength(50)]
    public required string UserIp { get; set; }
    
    /// <summary>
    /// Gets or sets the unique identifier of the server that the user joined.
    /// </summary>
    [StringLength(100)]
    public required string ServerId { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp indicating when the server join record was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp indicating when the server join record will expire.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; set; }
    
    /* ######################################################################
     *                         NAVIGATION PROPERTIES
     * ###################################################################### */
    
    /// <summary>
    /// Gets or sets the user associated with this server join record.
    /// </summary>
    [ForeignKey("UserId")]
    [JsonIgnore]
    public CustomUser? User { get; set; }
}
