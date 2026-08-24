using System.Net.Http.Json;
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

    public async Task<Stream> GenerateDocxAsync(string templateName, RenderRequestDto requestDto, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/generate/{templateName}")
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
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/task/{templateName}")
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
}