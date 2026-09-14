using System.ComponentModel.DataAnnotations;
using Tavstal.YggdrasilSharp.Models.Attributes;
using Tavstal.YggdrasilSharp.Models.Common;

namespace Tavstal.YggdrasilSharp.Models.Bodies.News;

/// <summary>
/// Represents the request body for creating a news entry.
/// </summary>
public class NewsCreateRequestBody
{
    /// <summary>
    /// Gets or sets the title of the news entry.
    /// This field is required.
    /// </summary>
    [Required]
    [MinLength(3)]
    [MaxLength(128)]
    public required string Title { get; set; }
        
    /// <summary>
    /// Gets or sets the content of the news entry.
    /// This field is required.
    /// </summary>
    [Required]
    [MinLength(3)]
    [MaxLength(512)]
    public required string Content { get; set; }
    
    /// <summary>
    /// Gets or sets the banner image for the news entry.
    /// This field is required.
    /// </summary>
    [Required]
    [FormFile(500, EFileSizeUnit.Kilobytes, ["image/png"], [".png"])]
    public required IFormFile Banner { get; set; }
}
