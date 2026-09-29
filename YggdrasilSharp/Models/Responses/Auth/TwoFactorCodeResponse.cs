using System.Text.Json.Serialization;

namespace Tavstal.YggdrasilSharp.Models.Responses.Auth;

public class TwoFactorCodeResponse : ResponseBase
{
    [JsonPropertyName("UserId")]
    public string UserId { get; set; } 
    
    [JsonPropertyName("Email")]
    public string Email { get; set; }
    
    [JsonPropertyName("RecoveryCodes")]
    public IEnumerable<string>? RecoveryCodes { get; set; }
}