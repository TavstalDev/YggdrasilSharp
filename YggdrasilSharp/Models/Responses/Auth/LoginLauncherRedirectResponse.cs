using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Responses.Auth;

public class LoginLauncherRedirectResponse : ResponseBase
{
    [JsonPropertyName("UserId")]
    public string UserId { get; set; } 
    
    [JsonPropertyName("Token")]
    public string? Token { get; set; } 
    
    [JsonPropertyName("Url")]
    public string Url { get; set; }
}