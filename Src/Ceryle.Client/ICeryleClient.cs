namespace Ceryle.Client;

public interface ICeryleClient
{
    Task<Stream> GenerateDocxAsync(string templateName,
        RenderRequestDto requestDto,
        CancellationToken ct = default);
    
    Task<Guid> CreateDocxRenderTaskAsync(string templateName,
        RenderRequestDto requestDto,
        CancellationToken ct = default);
    
    Task<Stream> DownloadDocxByTaskIdAsync(Guid taskId,
        CancellationToken ct = default);
}