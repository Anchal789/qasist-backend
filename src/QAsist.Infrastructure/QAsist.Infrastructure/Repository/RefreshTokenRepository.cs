using Dapper;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Domain.Entities;
using QAsist.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QAsist.Infrastructure.Repository
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public RefreshTokenRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new { p_token = token };
            return await connection.QueryFirstOrDefaultAsync<RefreshToken>(
                SqlQueries.RefreshTokens.GetByToken,
                parameters);
        }

        public async Task<RefreshToken?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new { p_session_id = sessionId };
            return await connection.QueryFirstOrDefaultAsync<RefreshToken>(
                SqlQueries.RefreshTokens.GetBySessionId,
                parameters);
        }

        public async Task<Guid> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new
            {
                p_id = refreshToken.Id,
                p_user_id = refreshToken.UserId,
                p_token = refreshToken.Token,
                p_session_id = refreshToken.SessionId,
                p_expires_at = refreshToken.ExpiresAt,
                p_ip_address = refreshToken.IpAddress,
                p_user_agent = refreshToken.UserAgent,
                p_created_by = refreshToken.UserId
            };

            return await connection.ExecuteScalarAsync<Guid>(
                SqlQueries.RefreshTokens.Create,
                parameters);
        }

        public async Task<bool> RevokeAsync(string token, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new { p_token = token };
            return await connection.ExecuteScalarAsync<bool>(
                SqlQueries.RefreshTokens.Revoke,
                parameters);
        }

        public async Task<bool> RevokeBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new { p_session_id = sessionId };
            return await connection.ExecuteScalarAsync<bool>(
                SqlQueries.RefreshTokens.RevokeBySessionId,
                parameters);
        }

        public async Task<bool> RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new { p_user_id = userId };
            return await connection.ExecuteScalarAsync<bool>(
                SqlQueries.RefreshTokens.RevokeAllByUserId,
                parameters);
        }

        public async Task<bool> DeleteExpiredAsync(CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var affectedRows = await connection.ExecuteAsync(
                SqlQueries.RefreshTokens.DeleteExpired);

            return affectedRows > 0;
        }
    }
}
