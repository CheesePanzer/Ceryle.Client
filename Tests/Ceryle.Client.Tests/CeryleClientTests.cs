using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ceryle.Client.Tests;

public class CeryleClientTests
{
    [Fact]
    public async Task GetHealthAsync_DeserializesResponse()
    {
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/health", request.RequestUri!.AbsolutePath);

            return JsonResponse("""
                {
                  "status": "Healthy",
                  "timestamp": 1718200000.123,
                  "datetime": "2026-06-13 12:00:00",
                  "service": "Ceryle",
                  "version": "1.0.0"
                }
                """);
        });

        var result = await CreateClient(handler).GetHealthAsync();

        Assert.Equal("Healthy", result.Status);
        Assert.Equal("Ceryle", result.Service);
        Assert.Equal("1.0.0", result.Version);
    }

    [Fact]
    public async Task GetTemplatesAsync_DeserializesTemplateNames()
    {
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/v1/templates", request.RequestUri!.AbsolutePath);
            return JsonResponse("[\"invoice.docx\",\"contract.docx\"]");
        });

        var templates = await CreateClient(handler).GetTemplatesAsync();

        Assert.Equal(["invoice.docx", "contract.docx"], templates);
    }

    [Fact]
    public async Task UploadTemplateAsync_SendsMultipartForm()
    {
        var handler = new StubHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/v1/templates/upload", request.RequestUri!.AbsolutePath);

            var body = await request.Content!.ReadAsStringAsync(ct);
            Assert.Contains("name=file", body);
            Assert.Contains("filename=sample.docx", body);
            Assert.Contains("document-content", body);
            Assert.Contains("name=overwrite", body);
            Assert.Contains("true", body);

            return JsonResponse("\"Successfully Saved Template sample.docx\"");
        });

        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("document-content"));
        var result = await CreateClient(handler).UploadTemplateAsync(content, "sample.docx", overwrite: true);

        Assert.Equal("Successfully Saved Template sample.docx", result);
    }

    [Fact]
    public async Task DeleteTemplateAsync_UsesPostAndEscapesTemplateName()
    {
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/v1/templates/my%20template.docx/delete", request.RequestUri!.AbsolutePath);
            return JsonResponse("\"Successfully Deleted Template my template.docx\"");
        });

        var result = await CreateClient(handler).DeleteTemplateAsync("my template.docx");

        Assert.Equal("Successfully Deleted Template my template.docx", result);
    }

    [Fact]
    public async Task GetTemplatesAsync_MapsProblemDetailsToCeryleApiException()
    {
        var handler = new StubHttpMessageHandler((_, _) => JsonResponse(
            """
            {
              "type": "about:blank",
              "title": "Service Error",
              "status": 404,
              "detail": "Template not found",
              "instance": "/api/v1/templates",
              "error_code": 404
            }
            """,
            HttpStatusCode.NotFound));

        var exception = await Assert.ThrowsAsync<CeryleApiException>(
            () => CreateClient(handler).GetTemplatesAsync());

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Equal("Service Error", exception.Title);
        Assert.Equal("Template not found", exception.Detail);
        Assert.Equal(404, exception.ErrorCode);
    }

    private static CeryleClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://ceryle.test")
        };
        return new CeryleClient(httpClient, NullLogger<CeryleClient>.Instance);
    }

    private static HttpResponseMessage JsonResponse(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        public StubHttpMessageHandler(
            Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
            : this((request, ct) => Task.FromResult(handler(request, ct)))
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return handler(request, cancellationToken);
        }
    }
}
