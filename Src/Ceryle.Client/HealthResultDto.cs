using System.Text.Json.Serialization;

namespace Ceryle.Client;

public sealed class HealthResultDto
{
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("timestamp")]
    public double Timestamp { get; init; }

    [JsonPropertyName("datetime")]
    public required string DateTime { get; init; }

    [JsonPropertyName("service")]
    public required string Service { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }
}
