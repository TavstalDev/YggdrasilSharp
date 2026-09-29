using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Responses.Auth;

public class TwoFactorSecretResponse : ResponseBase
{
    [JsonPropertyName("UserId")]
    public string UserId { get; set; } 
    
    [JsonPropertyName("Email")]
    public string Email { get; set; }
    
    [JsonPropertyName("Secret")]
    public string Secret { get; set; }
}