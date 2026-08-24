using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ceryle.Client;

public class ProblemDetails
{
    [JsonPropertyName("type")] 
    public string? Type { get; set; }

    [JsonPropertyName("title")] 
    public string? Title { get; set; }

    [JsonPropertyName("status")] 
    public int? Status { get; set; }

    [JsonPropertyName("detail")] 
    public string? Detail { get; set; }

    [JsonPropertyName("instance")] 
    public string? Instance { get; set; }

    [JsonExtensionData] 
    public Dictionary<string, JsonElement>? Extensions { get; set; }

    public int? GetErrorCode()
    {
        if (Extensions != null && Extensions.TryGetValue("error_code", out var val))
        {
            return val.GetInt32();
        }

        return null;
    }
}