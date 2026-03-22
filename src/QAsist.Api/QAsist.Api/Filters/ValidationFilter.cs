using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using QAsist.Application.Common.Responses;

namespace QAsist.Api.Filters
{
    public class ValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();

                var response = ApiResponse<object>.ErrorResponse(
                    ResponseMessages.ValidationFailed,
                    errors);

                response.CorrelationId = context.HttpContext.TraceIdentifier;

                context.Result = new BadRequestObjectResult(response);
                return;
            }

            await next();
        }
    }
}
