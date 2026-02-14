using AutoMapper;
using Microsoft.Extensions.Configuration;
using QAsist.Application.Common.Exceptions;
using QAsist.Application.Common.Responses;
using QAsist.Application.DTOs;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Application.Interfaces.IServices;
using QAsist.Domain.Entities;

namespace QAsist.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IJwtService _jwtService;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IJwtService jwtService,
            IMapper mapper,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _jwtService = jwtService;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto> LoginAsync(
            LoginRequestDto dto,
            string ipAddress,
            string userAgent,
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email, cancellationToken);

            if (user == null || !VerifyPassword(dto.Password, user.PasswordHash))
                throw new UnauthorizedException(ResponseMessages.InvalidCredentials);

            if (!user.IsActive)
                throw new ForbiddenException(ResponseMessages.UserDeactivated);

            var existingToken = await _refreshTokenRepository.GetBySessionIdAsync(dto.SessionId, cancellationToken);
            if (existingToken != null)
            {
                await _refreshTokenRepository.RevokeBySessionIdAsync(dto.SessionId, cancellationToken);
            }

            var accessToken = _jwtService.GenerateAccessToken(user, dto.SessionId);
            var refreshToken = _jwtService.GenerateRefreshToken();

            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                SessionId = dto.SessionId,
                ExpiresAt = DateTime.UtcNow.AddDays(
                    int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7")),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedBy = user.Id
            };

            await _refreshTokenRepository.CreateAsync(refreshTokenEntity, cancellationToken);
            await _userRepository.UpdateLastLoginAsync(user.Id, cancellationToken);

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15")),
                User = _mapper.Map<UserDto>(user)
            };
        }

        public async Task<LoginResponseDto> RefreshTokenAsync(
            RefreshTokenRequestDto dto,
            string ipAddress,
            string userAgent,
            CancellationToken cancellationToken = default)
        {
            var refreshToken = await _refreshTokenRepository.GetBySessionIdAsync(dto.SessionId, cancellationToken);

            if (refreshToken == null ||
                refreshToken.Token != dto.RefreshToken ||
                refreshToken.IsRevoked ||
                refreshToken.ExpiresAt < DateTime.UtcNow)
            {
                throw new UnauthorizedException(ResponseMessages.InvalidRefreshToken);
            }

            var user = await _userRepository.GetByIdAsync(refreshToken.UserId, cancellationToken);

            if (user == null || !user.IsActive)
                throw new UnauthorizedException(ResponseMessages.UserNotFound);

            await _refreshTokenRepository.RevokeAsync(refreshToken.Token, cancellationToken);

            var newAccessToken = _jwtService.GenerateAccessToken(user, dto.SessionId);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            var newRefreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = newRefreshToken,
                SessionId = dto.SessionId,
                ExpiresAt = DateTime.UtcNow.AddDays(
                    int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7")),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedBy = user.Id
            };

            await _refreshTokenRepository.CreateAsync(newRefreshTokenEntity, cancellationToken);

            return new LoginResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15")),
                User = _mapper.Map<UserDto>(user)
            };
        }

        public async Task LogoutAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            await _refreshTokenRepository.RevokeBySessionIdAsync(sessionId, cancellationToken);
        }

        public async Task LogoutAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            await _refreshTokenRepository.RevokeAllByUserIdAsync(userId, cancellationToken);
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash) || passwordHash.Length != 60)
                throw new UnauthorizedException(ResponseMessages.InvalidCredentials);

            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

    }
}
