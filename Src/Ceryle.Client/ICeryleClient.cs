namespace Ceryle.Client;

public interface ICeryleClient
{
    Task<HealthResultDto> GetHealthAsync(CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetTemplatesAsync(CancellationToken ct = default);

    Task<string> UploadTemplateAsync(Stream content,
        string fileName,
        bool overwrite = false,
        CancellationToken ct = default);

    Task<string> DeleteTemplateAsync(string templateName,
        CancellationToken ct = default);

    Task<Stream> GenerateDocxAsync(string templateName,
        RenderRequestDto requestDto,
        CancellationToken ct = default);
    
    Task<Guid> CreateDocxRenderTaskAsync(string templateName,
        RenderRequestDto requestDto,
        CancellationToken ct = default);
    
    Task<Stream> DownloadDocxByTaskIdAsync(Guid taskId,
        CancellationToken ct = default);
}
