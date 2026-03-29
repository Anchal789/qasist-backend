namespace QAsist.Api.Extensions;

public static class HttpClientExtensions
{
    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddHttpClient("OpenApiFetcher");
        return services;
    }
}
