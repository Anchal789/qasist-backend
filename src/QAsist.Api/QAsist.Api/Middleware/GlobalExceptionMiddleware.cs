using QAsist.Application.Common.Exceptions;
using QAsist.Application.Common.Responses;
using System.Net;
using System.Text.Json;

namespace QAsist.Api.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var correlationId = context.TraceIdentifier;

            _logger.LogError(exception,
                "An error occurred. CorrelationId: {CorrelationId}",
                correlationId);

            var statusCode = exception switch
            {
                NotFoundException => HttpStatusCode.NotFound,
                ValidationException => HttpStatusCode.BadRequest,
                BadRequestException => HttpStatusCode.BadRequest,
                UnauthorizedException => HttpStatusCode.Unauthorized,
                ForbiddenException => HttpStatusCode.Forbidden,
                ConflictException => HttpStatusCode.Conflict,
                _ => HttpStatusCode.InternalServerError
            };

            var message = exception switch
            {
                ValidationException or BadRequestException or NotFoundException
                    or UnauthorizedException or ForbiddenException or ConflictException
                    => exception.Message,
                _ => ResponseMessages.InternalServerError
            };

            var errors = exception is ValidationException validationException
                ? validationException.Errors
                : new List<string>();

            var response = new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Errors = errors,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}
