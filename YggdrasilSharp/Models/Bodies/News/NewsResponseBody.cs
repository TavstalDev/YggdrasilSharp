namespace Tavstal.YggdrasilSharp.Models.Bodies.News;

/// <summary>
/// Represents the response body for a news entry.
/// </summary>
public class NewsResponseBody
{
    /// <summary>
    /// Gets or sets the title of the news entry.
    /// </summary>
    public required string Title { get; set; }
    
    /// <summary>
    /// Gets or sets the content of the news entry.
    /// </summary>
    public required string Content { get; set; }
    
    /// <summary>
    /// Gets or sets the URL of the banner image for the news entry.
    /// </summary>
    public required string BannerUrl { get; set; }
}
