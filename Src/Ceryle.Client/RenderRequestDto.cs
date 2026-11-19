using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Ceryle.Client;

public record ImageDataDto(string VarName, string ImageSource, string? RealName)
{
    [JsonPropertyName("varName")] public string VarName { get; set; } = VarName;

    [JsonPropertyName("imageSource")] public string ImageSource { get; set; } = ImageSource;

    [JsonPropertyName("realName")] public string? RealName { get; set; } = RealName;
}

public class RenderRequestDto
{
    /// <summary>
    /// Expected Returned File Name
    /// </summary>
    [JsonPropertyName("expectName")]
    public string? ExpectName { get; set; }

    /// <summary>
    /// The Content that you want to render
    /// </summary>
    [JsonPropertyName("data")]
    public required JsonNode Data { get; set; }

    [JsonPropertyName("images")] public List<ImageDataDto>? Images { get; set; } = new();

    /// <summary>
    /// Creates a new <see cref="RenderRequestDto"/> instance by parsing a JSON string and optionally applying multiple merge option configurations in sequence.
    /// </summary>
    /// <param name="expectName">The expected file name or template identifier for rendering.</param>
    /// <param name="jsonData">The base JSON string representing the primary payload.</param>
    /// <param name="mergeOptionsList">A variable list of <see cref="JsonMergeOptions"/> applied sequentially to merge additional nodes.</param>
    /// <returns>A populated <see cref="RenderRequestDto"/> object.</returns>
    /// <exception cref="JsonException">Thrown when <paramref name="jsonData"/> cannot be parsed into a valid <see cref="JsonObject"/>.</exception>
    public static RenderRequestDto CreateFromJsonFormatString(
        string expectName,
        string jsonData,
        params JsonMergeOptions?[] mergeOptionsList)
    {
        var data = JsonNode.Parse(jsonData) as JsonObject
                   ?? throw new JsonException("Failed to parse to JsonObject: Invalid JSON or not an object");

        ApplyMergeOptions(data, mergeOptionsList);

        return new RenderRequestDto
        {
            ExpectName = expectName,
            Data = data
        };
    }

    /// <summary>
    /// Creates a new <see cref="RenderRequestDto"/> instance by serializing a strongly-typed object to a JSON node and optionally applying merge configurations.
    /// </summary>
    /// <typeparam name="T">The type of the primary data object.</typeparam>
    /// <param name="expectName">The expected file name or template identifier for rendering.</param>
    /// <param name="dataObject">The strong-typed object instance representing the primary payload.</param>
    /// <param name="jsonSerializerOptions">Optional custom <see cref="JsonSerializerOptions"/> for object serialization.</param>
    /// <param name="mergeOptionsList">A variable list of <see cref="JsonMergeOptions"/> applied sequentially to merge additional nodes.</param>
    /// <returns>A populated <see cref="RenderRequestDto"/> object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataObject"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when <paramref name="dataObject"/> fails to serialize into a valid <see cref="JsonObject"/>.</exception>
    public static RenderRequestDto CreateFromObject<T>(
        string expectName,
        T dataObject,
        JsonSerializerOptions? jsonSerializerOptions = null,
        params JsonMergeOptions?[] mergeOptionsList)
    {
        ArgumentNullException.ThrowIfNull(dataObject);

        var data = JsonSerializer.SerializeToNode(dataObject, jsonSerializerOptions) as JsonObject
                   ?? throw new JsonException($"Failed to serialize {typeof(T).Name} instance to JsonObject.");

        ApplyMergeOptions(data, mergeOptionsList);

        return new RenderRequestDto
        {
            ExpectName = expectName,
            Data = data
        };
    }

    private static void ApplyMergeOptions(JsonObject target, IEnumerable<JsonMergeOptions?> mergeOptionsList)
    {
        foreach (var mergeOptions in mergeOptionsList)
        {
            if (mergeOptions?.AdditionalData == null) continue;

            foreach (var node in mergeOptions.AdditionalData)
            {
                if (node == null) continue;

                foreach (var (key, value) in node)
                {
                    if (mergeOptions.Overwrite || !target.ContainsKey(key))
                    {
                        target[key] = value?.DeepClone();
                    }
                }
            }
        }
    }

    public void AddImage(ImageDataDto dto)
    {
        if (Images == null)
        {
            Images = new List<ImageDataDto>();
        }

        Images.Add(dto);
    }

    public void AddImages(IEnumerable<ImageDataDto> dtos)
    {
        if (Images == null)
        {
            Images = new List<ImageDataDto>();
        }

        Images.AddRange(dtos);
    }
}

public class JsonMergeOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to overwrite existing values when duplicate keys are encountered in additional nodes. Defaults to false.
    /// </summary>
    public bool Overwrite { get; set; } = false;

    /// <summary>
    /// Gets or sets the collection of additional JsonObject nodes to merge.
    /// </summary>
    public IEnumerable<JsonObject?>? AdditionalData { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="JsonMergeOptions"/> with specified overwrite behavior and additional nodes.
    /// </summary>
    public static JsonMergeOptions Create(bool overwrite, params JsonObject?[] additionalData) => new()
    {
        Overwrite = overwrite,
        AdditionalData = additionalData
    };
}
