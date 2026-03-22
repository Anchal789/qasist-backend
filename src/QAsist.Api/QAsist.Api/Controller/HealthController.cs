using Microsoft.AspNetCore.Mvc;
using QAsist.Application.Common.Responses;

namespace QAsist.Api.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<ApiResponse<object>>> GetHealthAsync()
        {
            var healthData = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                Version = "1.0.0"
            };

            var response = ApiResponse<object>.SuccessResponse(healthData, "Service is healthy");
            response.CorrelationId = HttpContext.TraceIdentifier;

            return await Task.FromResult(Ok(response));
        }
    }
}
