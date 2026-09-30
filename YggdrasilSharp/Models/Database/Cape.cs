using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tavstal.YggdrasilSharp.Models.Database;

/// <summary>
/// Represents a cape entity, including its unique identifier, name, associated file, 
/// visibility status, and navigation properties.
/// </summary>
public class Cape
{
    /// <summary>
    /// Gets or sets the unique identifier for the cape.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the cape.
    /// </summary>
    [StringLength(64)]
    public required string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the unique identifier of the file associated with the cape.
    /// </summary>
    public required ulong FileId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the cape is public.
    /// </summary>
    public bool IsPublic { get; set; }
    
    /* ######################################################################
     *                         NAVIGATION PROPERTIES
     * ###################################################################### */
    
    /// <summary>
    /// Gets or sets the file data associated with the cape.
    /// </summary>
    [ForeignKey("FileId")] 
    public FileData? FileData { get; set; }
}
