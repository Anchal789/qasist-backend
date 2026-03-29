using Serilog;

namespace QAsist.Api.Extensions;

public static class SerilogExtensions
{
    public static IServiceCollection AddSerilogConfig(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "QAsist")
            .WriteTo.Console()
            .WriteTo.File(
                path: "logs/qasist-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        return services;
    }
}
