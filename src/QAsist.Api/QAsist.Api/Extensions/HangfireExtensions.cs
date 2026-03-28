using Hangfire;
using Hangfire.PostgreSql;

namespace QAsist.Api.Extensions;

public static class HangfireExtensions
{
    public static IServiceCollection AddHangfireConfig(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHangfire(config =>
        {
            config
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(configuration.GetConnectionString("DefaultConnection"));
        });

        services.AddHangfireServer();

        return services;
    }
}
