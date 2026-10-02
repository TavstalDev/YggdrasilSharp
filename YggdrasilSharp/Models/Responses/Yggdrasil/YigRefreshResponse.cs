using System.Text.Json.Serialization;
using Tavstal.YggdrasilSharp.Models.Bodies.Yggdrasil;

namespace Tavstal.YggdrasilSharp.Models.Responses.Yggdrasil;

public class YigRefreshResponse
{
    [JsonPropertyName("accessToken")]
    public required string AccessToken { get; set; }
    
    [JsonPropertyName("clientToken")]
    public required string ClientToken { get; set; }
    
    [JsonPropertyName("selectedProfile")]
    public required YigProfileBody SelectedProfile { get; set; }
}