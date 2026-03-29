using Microsoft.Extensions.DependencyInjection;
using QAsist.Application.Execution;
using QAsist.Application.Execution.Assertions.Assertors;
using QAsist.Application.Execution.Assertions;
using QAsist.Application.Interfaces.IContext;
using QAsist.Application.Interfaces.IContext.IAssertions;
using QAsist.Application.Interfaces.IServices;
using QAsist.Application.Services;
using System.Reflection;
using QAsist.Application.Execution.Extractions;
using QAsist.Infrastructure.Monitoring;

namespace QAsist.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<ITestCaseGeneratorService, TestCaseGeneratorService>();
            services.AddScoped<IEnvironmentService, EnvironmentService>();
            services.AddScoped<ITestExecutionService, TestExecutionService>();
            services.AddScoped<ITestCaseService, TestCaseService>();

            services.AddScoped<IVariableResolver, VariableResolver>();
            // Register each assertor individually (all implement IAssertor)
            // AssertionEngine receives IEnumerable<IAssertor> — gets ALL of them
            services.AddScoped<IAssertor, StatusCodeAssertor>();
            services.AddScoped<IAssertor, ResponseTimeAssertor>();
            services.AddScoped<IAssertor, BodyContainsAssertor>();
            services.AddScoped<IAssertionEngine, AssertionEngine>();
            services.AddScoped<IAssertor, FieldEqualsAssertor>();
            services.AddScoped<IAssertor, FieldExistsAssertor>();
            services.AddScoped<IAssertor, FieldNotExistsAssertor>();
            services.AddScoped<IAssertor, FieldMatchesRegexAssertor>();

            // ── Assertion Engine facade (registered AFTER all assertors) ──────
            services.AddScoped<IAssertionEngine, AssertionEngine>();

            services.AddScoped<IExtractor, BodyExtractor>();
            services.AddScoped<IExtractor, HeaderExtractor>();
            services.AddScoped<IExtractor, StatusCodeExtractor>();
            services.AddScoped<IExtractionEngine, ExtractionEngine>();
            services.AddScoped<ITestSuiteService, TestSuiteService>();
            services.AddScoped<IExecutionService, ExecutionService>();

            services.AddScoped<ISuiteExecutionService, SuiteExecutionService>();
            services.AddScoped<ITestSuiteMappingService, TestSuiteMappingService>();

            // Phase 3: Monitoring
            services.AddScoped<IUptimeCalculator, UptimeCalculator>(); 
            return services;
        }
    }
}
