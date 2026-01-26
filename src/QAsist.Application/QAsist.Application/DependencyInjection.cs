using Microsoft.Extensions.DependencyInjection;
using QAsist.Application.Interfaces.IServices;
using QAsist.Application.Services;
using System.Reflection;

namespace QAsist.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            services.AddScoped<IProjectService, ProjectService>();

            return services;
        }
    }
}
