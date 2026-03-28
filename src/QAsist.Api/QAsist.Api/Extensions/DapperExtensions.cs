using Dapper;

namespace QAsist.Api.Extensions;

public static class DapperExtensions
{
    public static IServiceCollection AddDapperConfig(this IServiceCollection services)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        return services;
    }
}