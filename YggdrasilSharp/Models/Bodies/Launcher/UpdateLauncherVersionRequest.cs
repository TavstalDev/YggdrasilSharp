using System.ComponentModel.DataAnnotations;
using Tavstal.YggdrasilSharp.Models.Database.Launcher;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Launcher;

/// <summary>
/// Represents the request body for updating an existing launcher version.
/// </summary>
public class UpdateLauncherVersionRequest
{
    /// <summary>
    /// Gets or sets the version of the launcher.
    /// This field is optional and must be a valid semantic version string with a maximum length of 15 characters.
    /// </summary>
    [StringLength(15)]
    [RegularExpression("^(?:(\\d+)\\.)?(?:(\\d+)\\.)?(\\*|\\d+)$\n")]
    public string? Version { get; set; }
    
    /// <summary>
    /// Gets or sets the type of the launcher version.
    /// This field is optional.
    /// </summary>
    public EVersionType? VersionType { get; set; }
    
    /// <summary>
    /// Gets or sets the changelog for the launcher version.
    /// This field is optional and must not exceed 500 characters.
    /// </summary>
    [StringLength(500)]
    public string? Changelog { get; set; }
}