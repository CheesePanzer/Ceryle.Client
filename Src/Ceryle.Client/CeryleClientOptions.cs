namespace Ceryle.Client;

public class CeryleClientOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public int ClientTimeoutSecs { get; set; } = 30;
    public string ApiKey { get; set; } = string.Empty;
}