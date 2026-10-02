using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Responses.User;

/// <summary>
/// Represents the publicly available information of a user.
/// </summary>
public class UserInfoResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the user.
    /// </summary>
    [JsonPropertyName("UserId")]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the username of the user.
    /// </summary>
    [JsonPropertyName("UserName")]
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the URL or path to the avatar of the user.
    /// </summary>
    [JsonPropertyName("AvatarUrl")]
    public string? AvatarUrl { get; set; }
    
    /// <summary>
    /// Gets or sets the Discord identifier linked to the user.
    /// </summary>
    [JsonPropertyName("DiscordId")]
    public ulong? DiscordId { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time at which the user account was created.
    /// </summary>
    [JsonPropertyName("CreateDate")]
    public DateTimeOffset CreateDate { get; set; } 
    
    /// <summary>
    /// Gets or sets the date and time at which the user account was last updated.
    /// </summary>
    [JsonPropertyName("LastUpdate")]
    public DateTimeOffset LastUpdate { get; set; }
}
