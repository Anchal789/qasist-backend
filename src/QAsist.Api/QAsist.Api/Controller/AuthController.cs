using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QAsist.Api.Extensions;
using QAsist.Application.Common.Responses;
using QAsist.Application.DTOs;
using QAsist.Application.Interfaces.IServices;

namespace QAsist.Api.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> LoginAsync(
            [FromBody] LoginRequestDto dto,
            CancellationToken cancellationToken)
        {
            var ipAddress = HttpContext.GetIpAddress();
            var userAgent = HttpContext.GetUserAgent();

            var result = await _authService.LoginAsync(dto, ipAddress, userAgent, cancellationToken);

            var response = ApiResponse<LoginResponseDto>.SuccessResponse(
                result,
                ResponseMessages.LoginSuccessful);

            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> RefreshTokenAsync(
            [FromBody] RefreshTokenRequestDto dto,
            CancellationToken cancellationToken)
        {
            var ipAddress = HttpContext.GetIpAddress();
            var userAgent = HttpContext.GetUserAgent();

            var result = await _authService.RefreshTokenAsync(dto, ipAddress, userAgent, cancellationToken);

            var response = ApiResponse<LoginResponseDto>.SuccessResponse(
                result,
                ResponseMessages.TokenRefreshedSuccessfully);

            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> LogoutAsync(
            [FromBody] string sessionId,
            CancellationToken cancellationToken)
        {
            await _authService.LogoutAsync(sessionId, cancellationToken);

            var response = ApiResponse<object>.SuccessResponse(
                null,
                ResponseMessages.LogoutSuccessful);

            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        [HttpPost("logout-all")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> LogoutAllSessionsAsync(
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            await _authService.LogoutAllSessionsAsync(userId, cancellationToken);

            var response = ApiResponse<object>.SuccessResponse(
                null,
                "All sessions logged out successfully");

            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<RegisterResponseDto>>> RegisterAsync(
        [FromBody] RegisterRequestDto dto)
        {
            await _authService.RegisterAsync(dto);

            var response = ApiResponse<RegisterResponseDto>.SuccessResponse(
                null,
                "User registered successfully");

            response.CorrelationId = HttpContext.TraceIdentifier;

            return Ok(response);
        }
    }
}

