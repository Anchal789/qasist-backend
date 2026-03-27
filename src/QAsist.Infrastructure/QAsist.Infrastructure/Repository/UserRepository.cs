using Dapper;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Domain.Entities;
using QAsist.Infrastructure.Persistence;

namespace QAsist.Infrastructure.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public UserRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new { p_id = id };
            return await connection.QueryFirstOrDefaultAsync<User>(
                SqlQueries.Users.GetById,
                parameters);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new { p_email = email };
            return await connection.QueryFirstOrDefaultAsync<User>(
                SqlQueries.Users.GetByEmail,
                parameters);
        }

        public async Task<Guid> CreateAsync(User user, Guid createdBy, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new
            {
                p_id = user.Id,
                p_email = user.Email,
                p_first_name = user.FirstName,
                p_last_name = user.LastName,
                p_password_hash = user.PasswordHash,
                p_role = user.Role,
                p_created_by = createdBy
            };

            return await connection.ExecuteScalarAsync<Guid>(
                SqlQueries.Users.Create,
                parameters);
        }

        public async Task<bool> UpdateAsync(User user, Guid userId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new
            {
                p_id = user.Id,
                p_email = user.Email,
                p_first_name = user.FirstName,
                p_last_name = user.LastName,
                p_role = user.Role,
                p_is_active = user.IsActive,
                p_updated_by = userId
            };

            return await connection.ExecuteScalarAsync<bool>(
                SqlQueries.Users.Update,
                parameters);
        }

        public async Task<bool> ExistsByEmailAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new
            {
                p_email = email,
                p_exclude_id = excludeId
            };

            return await connection.ExecuteScalarAsync<bool>(
                SqlQueries.Users.ExistsByEmail,
                parameters);
        }

        public async Task UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new { p_user_id = userId };
            await connection.ExecuteAsync(
                SqlQueries.Users.UpdateLastLogin,
                parameters);
        }

        public async Task<Guid> AddAsync(User user, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new
            {
                p_id = user.Id,
                p_email = user.Email,
                p_first_name = user.FirstName,
                p_last_name = user.LastName,
                p_password_hash = user.PasswordHash,
                p_role = user.Role,
                p_created_by = user.CreatedBy
            };

            return await connection.ExecuteScalarAsync<Guid>(
                SqlQueries.Users.Add,
                parameters);
        }

        public Task AddAsync(User user)
        {
            throw new NotImplementedException();
        }
    }
}
