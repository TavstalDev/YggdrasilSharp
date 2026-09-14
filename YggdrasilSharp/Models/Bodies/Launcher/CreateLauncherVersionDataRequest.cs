using System.ComponentModel.DataAnnotations;
using Tavstal.YggdrasilSharp.Models.Attributes;
using Tavstal.YggdrasilSharp.Models.Database.Launcher;

namespace Tavstal.YggdrasilSharp.Models.Bodies.Launcher;

/// <summary>
/// Represents the request body for creating a new launcher version data.
/// </summary>
public class CreateLauncherVersionDataRequest
{
    /// <summary>
    /// Gets or sets the operating system for which the launcher version is intended.
    /// This field is required.
    /// </summary>
    [Required]
    public required ELauncherOs Os { get; set; }
    
    /// <summary>
    /// Gets or sets the file containing the launcher version data.
    /// This field is required and must be a compressed file with a maximum size of 512 kilobytes.
    /// Supported file extensions are .zip, .tar.gz, and .tar.
    /// Supported content types are application/zip, application/gzip, and application/x-tar.
    /// </summary>
    [Required]
    [FormFile(maxFileSize: 512, fileExtensions: [".zip", ".tar.gz", ".tar"], contentTypes: ["application/zip", "application/gzip", "application/x-tar"])]
    public required IFormFile File { get; set; }
}
