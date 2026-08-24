using Microsoft.Extensions.DependencyInjection;

namespace Ceryle.Client;

public static class CeryleServiceCollectionExtensions
{
    public static IServiceCollection AddCeryleClient(
        this IServiceCollection services,
        Action<CeryleClientOptions>? configureOptions = null)
    {
        var options = new CeryleClientOptions();

        configureOptions?.Invoke(options);

        services.AddHttpClient<ICeryleClient, CeryleClient>((client) =>
        {
            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                client.BaseAddress = new Uri(options.BaseUrl);
            }

            client.Timeout = TimeSpan.FromSeconds(options.ClientTimeoutSecs);

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Remove("X-Api-Key");
                client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
            }
        });

        return services;
    }
}