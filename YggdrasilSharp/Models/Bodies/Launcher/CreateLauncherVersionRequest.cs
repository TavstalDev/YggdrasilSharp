using System.ComponentModel.DataAnnotations;
using Tavstal.YggdrasilSharp.Models.Database.Launcher;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Launcher;

/// <summary>
/// Represents the request body for creating a new launcher version.
/// </summary>
public class CreateLauncherVersionRequest
{
    /// <summary>
    /// Gets or sets the version of the launcher.
    /// This field is required and must be a valid semantic version string with a maximum length of 15 characters.
    /// </summary>
    [Required]
    [StringLength(15)]
    [RegularExpression("^(?:(\\d+)\\.)?(?:(\\d+)\\.)?(\\*|\\d+)$\n")]
    public required string Version { get; set; }
    
    /// <summary>
    /// Gets or sets the type of the launcher version.
    /// This field is required.
    /// </summary>
    [Required]
    public required EVersionType VersionType { get; set; }
    
    /// <summary>
    /// Gets or sets the changelog for the launcher version.
    /// This field is required and must not exceed 500 characters.
    /// </summary>
    [Required]
    [StringLength(500)]
    public required string Changelog { get; set; }
}
