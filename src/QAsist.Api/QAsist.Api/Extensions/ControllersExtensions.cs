using QAsist.Api.Filters;

namespace QAsist.Api.Extensions;

public static class ControllersExtensions
{
    public static IServiceCollection AddControllersWithValidation(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
        });

        return services;
    }
}
