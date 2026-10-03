using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Database;

/// <summary>
/// Represents a news entry, including its title, content, banner, and creation timestamp.
/// </summary>
public class News
{
    /// <summary>
    /// Gets or sets the unique identifier for the news entry.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }
    
    /// <summary>
    /// Gets or sets the title of the news entry.
    /// The title has a maximum length of 128 characters.
    /// </summary>
    [StringLength(128)]
    public required string Title { get; set; }
    
    /// <summary>
    /// Gets or sets the content of the news entry.
    /// The content has a maximum length of 512 characters.
    /// </summary>
    [StringLength(512)]
    public required string Content { get; set; }
    
    /// <summary>
    /// Gets or sets the unique identifier of the banner associated with the news entry.
    /// </summary>
    public required ulong BannerId { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp indicating when the news entry was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }
    
    /* ######################################################################
     *                         NAVIGATION PROPERTIES
     * ###################################################################### */
    
    /// <summary>
    /// Gets or sets the banner file associated with the news entry.
    /// </summary>
    [ForeignKey("BannerId")]
    [JsonIgnore]
    public FileData? Banner { get; set; }
}
