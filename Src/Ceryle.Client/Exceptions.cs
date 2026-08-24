using System.Net;

namespace Ceryle.Client;

public class CeryleApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? Title { get; }
    public string? Detail { get; }
    public string? Instance { get; }
    public int? ErrorCode { get; }
    public string? Type { get; }

    public CeryleApiException(
        HttpStatusCode statusCode,
        string? detail,
        string? title = null,
        int? errorCode = null,
        string? instance = null,
        string? type = null,
        Exception? innerException = null)
        : base(FormatMessage(statusCode, title, detail, errorCode), innerException)
    {
        StatusCode = statusCode;
        Detail = detail;
        Title = title;
        ErrorCode = errorCode;
        Instance = instance;
        Type = type;
    }

    private static string FormatMessage(HttpStatusCode statusCode, string? title, string? detail, int? errorCode)
    {
        var prefix = errorCode is null ? $"[{(int)statusCode}]" : $"[{(int)statusCode} | {errorCode}]";
        var headline = string.IsNullOrEmpty(title) ? statusCode.ToString() : title;
        return $"{prefix} {headline}: {detail ?? "No additional details provided."}";
    }
}