using System.Net.Http.Json;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Ceryle.Client;

public class CeryleClient : ICeryleClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CeryleClient> _logger;

    public CeryleClient(HttpClient httpClient, ILogger<CeryleClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<HealthResultDto> GetHealthAsync(CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync("/health", ct);
        await EnsureSuccessAsync(response, ct);

        return await response.Content.ReadFromJsonAsync<HealthResultDto>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Ceryle returned an empty health response.");
    }

    public async Task<IReadOnlyList<string>> GetTemplatesAsync(CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync("/api/v1/templates", ct);
        await EnsureSuccessAsync(response, ct);

        return await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken: ct) ?? [];
    }

    public async Task<string> UploadTemplateAsync(
        Stream content,
        string fileName,
        bool overwrite = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(content);
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(overwrite.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()), "overwrite");

        using var response = await _httpClient.PostAsync("/api/v1/templates/upload", form, ct);
        await EnsureSuccessAsync(response, ct);

        return await ReadRequiredJsonStringAsync(response, ct);
    }

    public async Task<string> DeleteTemplateAsync(string templateName, CancellationToken ct = default)
    {
        using var response = await _httpClient.PostAsync(
            $"/api/v1/templates/{EscapePathSegment(templateName)}/delete",
            content: null,
            ct);
        await EnsureSuccessAsync(response, ct);

        return await ReadRequiredJsonStringAsync(response, ct);
    }

    public async Task<Stream> GenerateDocxAsync(string templateName, RenderRequestDto requestDto, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/generate/{EscapePathSegment(templateName)}")
        {
            Content = JsonContent.Create(requestDto)
        };

        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        _logger.LogInformation("Sending request: {Payload}", JsonSerializer.Serialize(requestDto, options));
        
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, ct);
        }
        
        return await response.Content.ReadAsStreamAsync(ct);
    }

    public async Task<Guid> CreateDocxRenderTaskAsync(string templateName, RenderRequestDto requestDto, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/task/{EscapePathSegment(templateName)}")
        {
            Content = JsonContent.Create(requestDto)
        };

        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        _logger.LogInformation("Sending request: {Payload}", JsonSerializer.Serialize(requestDto, options));

        using var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, ct);
        }

        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task<Stream> DownloadDocxByTaskIdAsync(Guid taskId, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/task/file/{taskId}");

        // 采用 ResponseHeadersRead 开启大文件流式传输
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, ct);
        }

        return await response.Content.ReadAsStreamAsync(ct);
    }

    private async Task HandleErrorResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var errorJson = await response.Content.ReadAsStringAsync(ct);

            var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogError(
                "Request failed [{StatusCode}]. ErrorCode: {ErrorCode}, Title: {Title}, Detail: {Detail}",
                response.StatusCode,
                problem?.GetErrorCode(),
                problem?.Title,
                problem?.Detail ?? errorJson
            );

            throw new CeryleApiException(
                statusCode: response.StatusCode,
                detail: problem?.Detail ?? errorJson,
                title: problem?.Title,
                errorCode: problem?.GetErrorCode(),
                instance: problem?.Instance,
                type: problem?.Type
            );
        }
        finally
        {
            response.Dispose();
        }
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, ct);
        }
    }

    private static async Task<string> ReadRequiredJsonStringAsync(HttpResponseMessage response, CancellationToken ct)
    {
        return await response.Content.ReadFromJsonAsync<string>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Ceryle returned an empty response.");
    }

    private static string EscapePathSegment(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Uri.EscapeDataString(value);
    }
}
