using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tavstal.YggdrasilSharp.Models.Database.Launcher;

/// <summary>
/// Represents the data associated with a specific launcher version, including its operating system, 
/// related file, and timestamps for creation and updates.
/// </summary>
public class LauncherVersionData
{
    /// <summary>
    /// Gets or sets the unique identifier for the launcher version data.
    /// </summary>
    [Key]
    public ulong Id { get; set; }
    
    /// <summary>
    /// Gets or sets the identifier of the associated launcher version.
    /// </summary>
    public ulong VersionId { get; set; }
    
    /// <summary>
    /// Gets or sets the identifier of the associated file.
    /// </summary>
    public ulong FileId { get; set; }
    
    /// <summary>
    /// Gets or sets the operating system supported by this launcher version.
    /// </summary>
    public ELauncherOs Os { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp indicating when this launcher version data was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp indicating when this launcher version data was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
    
    /* ######################################################################
     *                         NAVIGATION PROPERTIES
     * ###################################################################### */
    
    /// <summary>
    /// Gets or sets the associated launcher version.
    /// </summary>
    [ForeignKey("VersionId")]
    public LauncherVersion? Version { get; set; }
    
    /// <summary>
    /// Gets or sets the associated file data.
    /// </summary>
    [ForeignKey("FileId")]
    public FileData? File { get; set; }
}
