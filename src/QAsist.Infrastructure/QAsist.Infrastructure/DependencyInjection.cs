using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Application.Interfaces.IServices;
using QAsist.Infrastructure.Persistence;
using QAsist.Infrastructure.Repository;
using QAsist.Infrastructure.Services;

namespace QAsist.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ITestCaseRepository, TestCaseRepository>();

        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITestCaseExportService, TestCaseExcelExportService>();

        services.AddScoped<IEnvironmentRepository, EnvironmentRepository>();
        // AI Service - conditionally register based on configuration
        var aiProvider = configuration["AI:Provider"]?.ToLower() ?? "mock";

        if (aiProvider == "openai")
        {
            services.AddScoped<IAiService, OpenAiService>();
        }
        else
        {
            services.AddScoped<IAiService, MockAiService>();
        }

        return services;
    }
}