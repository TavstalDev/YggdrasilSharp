using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tavstal.YggdrasilSharp.Models.Database.Launcher;

/// <summary>
/// Represents a version of the launcher, including its details such as version number, type, changelog, and timestamps.
/// </summary>
public class LauncherVersion
{
    /// <summary>
    /// Gets or sets the unique identifier for the launcher version.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }
    
    /// <summary>
    /// Gets or sets the version number of the launcher.
    /// The version number must follow the semantic versioning format.
    /// </summary>
    [StringLength(15)]
    [RegularExpression("^(?:(\\d+)\\.)?(?:(\\d+)\\.)?(\\*|\\d+)$\n")]
    public required string Version { get; set; }
    
    /// <summary>
    /// Gets or sets the type of the launcher version (e.g., Alpha, Beta, Release).
    /// </summary>
    public required EVersionType VersionType { get; set; }
    
    /// <summary>
    /// Gets or sets the changelog describing the updates or changes in this version.
    /// </summary>
    [StringLength(500)]
    public required string Changelog { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp indicating when the launcher version was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp indicating when the launcher version was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
