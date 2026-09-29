using System.Text.Json.Serialization;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Tavstal.YggdrasilSharp.Models.Responses.Auth;

public class LoginResponse : ResponseBase
{
    [JsonPropertyName("UserId")]
    public string UserId { get; set; } 
    
    [JsonPropertyName("Token")]
    public string? Token { get; set; } 
    
    [JsonPropertyName("Expires")]
    public string Expires { get; set; } 
}
