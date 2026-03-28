using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace QAsist.Api.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimitingConfig(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // General: 100 requests per minute per IP
            options.AddFixedWindowLimiter("fixed", limiterOptions =>
            {
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.PermitLimit = 100;
                limiterOptions.QueueLimit = 10;
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            // AI endpoints: stricter at 20 per minute
            options.AddFixedWindowLimiter("ai_strict", limiterOptions =>
            {
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.PermitLimit = 20;
                limiterOptions.QueueLimit = 5;
            });

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var response = new
                {
                    success = false,
                    message = "Too many requests. Please slow down.",
                    retryAfter = "60 seconds"
                };

                await context.HttpContext.Response.WriteAsJsonAsync(response, token);
            };
        });

        return services;
    }
}
