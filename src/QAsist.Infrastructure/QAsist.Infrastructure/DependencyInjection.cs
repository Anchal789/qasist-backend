//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using QAsist.Application.Interfaces.IRepositories;
//using QAsist.Application.Interfaces.IServices;
//using QAsist.Infrastructure.Persistence;
//using QAsist.Infrastructure.Repository;
//using QAsist.Infrastructure.Services;

//namespace QAsist.Infrastructure;

//public static class DependencyInjection
//{
//    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
//    {
//        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

//        services.AddScoped<IProjectRepository, ProjectRepository>();
//        services.AddScoped<IUserRepository, UserRepository>();
//        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
//        services.AddScoped<ITestCaseRepository, TestCaseRepository>();

//        //services.AddScopped<IExecutionRepository>();
//        services.AddScoped<IJwtService, JwtService>();
//        services.AddScoped<IAuthService, AuthService>();
//        services.AddScoped<ITestCaseExportService, TestCaseExcelExportService>();

//        services.AddScoped<IEnvironmentRepository, EnvironmentRepository>();
//        // AI Service - conditionally register based on configuration
//        var aiProvider = configuration["AI:Provider"]?.ToLower() ?? "mock";

//        if (aiProvider == "openai")
//        {
//            services.AddScoped<IAiService, OpenAiService>();
//        }
//        else
//        {
//            services.AddScoped<IAiService, MockAiService>();
//        }

//        return services;
//    }
//}


// ── ADD to QAsist.Infrastructure/DependencyInjection.cs ──────────────────────
//
// 1. Install these packages in QAsist.Infrastructure:
//    dotnet add package Microsoft.Extensions.Http.Polly
//    dotnet add package Polly
//
// 2. Add using statements:
//    using Microsoft.Extensions.Http;
//    using Polly;
//    using Polly.Extensions.Http;
//
// 3. Replace your existing AddInfrastructure() with the version below.
// ─────────────────────────────────────────────────────────────────────────────

using Microsoft.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using QAsist.Application.Execution;
using QAsist.Application.Execution.Auth;
using QAsist.Application.Execution.Http;
using QAsist.Application.Interfaces.IContext;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Application.Interfaces.IServices;
using QAsist.Infrastructure.Persistence;
using QAsist.Infrastructure.Repository;
using QAsist.Infrastructure.Services;

namespace QAsist.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ── DB ────────────────────────────────────────────────────────────
            services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

            // ── Repositories ──────────────────────────────────────────────────
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<ITestCaseRepository, TestCaseRepository>();
            services.AddScoped<IEnvironmentRepository, EnvironmentRepository>();

            // ── Services ──────────────────────────────────────────────────────
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITestCaseExportService, TestCaseExcelExportService>();

            // AI
            var aiProvider = configuration["AI:Provider"]?.ToLower() ?? "mock";
            services.AddScoped<IAiService>(sp =>
                aiProvider == "openai"
                    ? sp.GetRequiredService<OpenAiService>()
                    : sp.GetRequiredService<MockAiService>());
            services.AddScoped<OpenAiService>();
            services.AddScoped<MockAiService>();

            // ── Week 6: HTTP Layer ────────────────────────────────────────────
            services.AddScoped<IRequestBuilder, RequestBuilder>();
            services.AddScoped<IResponseReader, ResponseReader>();

            // Named HttpClient "StepExecutor" with Polly retry + circuit breaker
            services.AddHttpClient("StepExecutor", client =>
            {
                client.DefaultRequestHeaders.Add(
                    "User-Agent", "QAsist-Engine/1.0");
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

            // ── Week 7: Auth + Executors ──────────────────────────────────────
            services.AddScoped<AuthInjector>();
            services.AddScoped<IStepExecutor, StepExecutor>();
            services.AddScoped<ISuiteExecutor, SuiteExecutor>();

            return services;
        }

        // ── Polly Retry: 3 retries with exponential backoff ───────────────────
        // Retries on: 5xx status codes + network errors (transient)
        // Delays: 1s, 2s, 4s
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)),
                    onRetry: (outcome, delay, attempt, _) =>
                    {
                        Console.WriteLine(
                            $"[Polly] Retry {attempt} after {delay.TotalSeconds}s. " +
                            $"Reason: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
                    });

        // ── Polly Circuit Breaker: open after 5 failures, reset after 30s ────
        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (_, duration) =>
                        Console.WriteLine(
                            $"[Polly] Circuit OPEN for {duration.TotalSeconds}s."),
                    onReset: () =>
                        Console.WriteLine("[Polly] Circuit CLOSED."));
    }
}