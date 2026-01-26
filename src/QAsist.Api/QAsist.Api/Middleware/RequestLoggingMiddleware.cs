using System.Diagnostics;

namespace QAsist.Api.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            var requestPath = context.Request.Path;
            var requestMethod = context.Request.Method;
            var correlationId = context.TraceIdentifier;

            _logger.LogInformation(
                "Starting request {Method} {Path} - CorrelationId: {CorrelationId}",
                requestMethod,
                requestPath,
                correlationId);

            await _next(context);

            stopwatch.Stop();

            _logger.LogInformation(
                "Completed request {Method} {Path} - Status: {StatusCode} - Duration: {Duration}ms - CorrelationId: {CorrelationId}",
                requestMethod,
                requestPath,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId);
        }
    }
}
