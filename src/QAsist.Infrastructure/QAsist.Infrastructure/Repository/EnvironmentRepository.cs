using Dapper;
using QAsist.Infrastructure.Persistence;
using QAsist.Application.Interfaces.IRepositories;
using System.Text.Json;
using Environment = QAsist.Domain.Entities.Environment;

namespace QAsist.Infrastructure.Repository
{
    public class EnvironmentRepository : IEnvironmentRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public EnvironmentRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Environment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string sql = @"
                SELECT id, project_id, name, base_url, global_headers, 
                       is_production, allow_execution, created_at, updated_at, 
                       created_by, updated_by
                FROM environments
                WHERE id = @Id AND deleted_at IS NULL";

            var result = await connection.QueryFirstOrDefaultAsync<EnvironmentDb>(sql, new { Id = id });
            return result != null ? MapToEnvironment(result) : null;
        }

        public async Task<IEnumerable<Environment>> GetByProjectAsync(
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string sql = @"
                SELECT id, project_id, name, base_url, global_headers, 
                       is_production, allow_execution, created_at, updated_at,
                       created_by, updated_by
                FROM environments
                WHERE project_id = @ProjectId AND deleted_at IS NULL
                ORDER BY is_production, name";

            var results = await connection.QueryAsync<EnvironmentDb>(sql, new { ProjectId = projectId });
            return results.Select(MapToEnvironment);
        }

        public async Task<Environment?> GetByNameAsync(
            Guid projectId,
            string name,
            CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string sql = @"
                SELECT id, project_id, name, base_url, global_headers, 
                       is_production, allow_execution, created_at, updated_at,
                       created_by, updated_by
                FROM environments
                WHERE project_id = @ProjectId AND name = @Name AND deleted_at IS NULL";

            var result = await connection.QueryFirstOrDefaultAsync<EnvironmentDb>(
                sql,
                new { ProjectId = projectId, Name = name });

            return result != null ? MapToEnvironment(result) : null;
        }

        public async Task<Guid> CreateAsync(
            Environment environment,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string sql = @"
                INSERT INTO environments 
                (id, project_id, name, base_url, global_headers, is_production, 
                 allow_execution, created_at, created_by)
                VALUES 
                (@Id, @ProjectId, @Name, @BaseUrl, @GlobalHeaders::jsonb, @IsProduction,
                 @AllowExecution, @CreatedAt, @CreatedBy)
                RETURNING id";

            var globalHeadersJson = JsonSerializer.Serialize(environment.GlobalHeaders);

            return await connection.ExecuteScalarAsync<Guid>(sql, new
            {
                environment.Id,
                environment.ProjectId,
                environment.Name,
                environment.BaseUrl,
                GlobalHeaders = globalHeadersJson,
                environment.IsProduction,
                environment.AllowExecution,
                environment.CreatedAt,
                CreatedBy = userId
            });
        }

        public async Task<bool> UpdateAsync(
            Environment environment,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string sql = @"
                UPDATE environments
                SET name = @Name,
                    base_url = @BaseUrl,
                    global_headers = @GlobalHeaders::jsonb,
                    is_production = @IsProduction,
                    allow_execution = @AllowExecution,
                    updated_at = @UpdatedAt,
                    updated_by = @UpdatedBy
                WHERE id = @Id AND deleted_at IS NULL
                RETURNING TRUE";

            var globalHeadersJson = JsonSerializer.Serialize(environment.GlobalHeaders);

            var result = await connection.ExecuteScalarAsync<bool?>(sql, new
            {
                environment.Id,
                environment.Name,
                environment.BaseUrl,
                GlobalHeaders = globalHeadersJson,
                environment.IsProduction,
                environment.AllowExecution,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userId
            });

            return result ?? false;
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string sql = @"
                UPDATE environments
                SET deleted_at = @DeletedAt,
                    deleted_by = @DeletedBy
                WHERE id = @Id AND deleted_at IS NULL
                RETURNING TRUE";

            var result = await connection.ExecuteScalarAsync<bool?>(sql, new
            {
                Id = id,
                DeletedAt = DateTime.UtcNow,
                DeletedBy = userId
            });

            return result ?? false;
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string sql = @"
                SELECT EXISTS(
                    SELECT 1 FROM environments 
                    WHERE id = @Id AND deleted_at IS NULL
                )";

            return await connection.ExecuteScalarAsync<bool>(sql, new { Id = id });
        }

        private static Environment MapToEnvironment(EnvironmentDb db)
        {
            Dictionary<string, string> globalHeaders;
            try
            {
                globalHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(db.GlobalHeaders ?? "{}")
                    ?? new Dictionary<string, string>();
            }
            catch
            {
                globalHeaders = new Dictionary<string, string>();
            }

            return new Environment
            {
                Id = db.Id,
                ProjectId = db.ProjectId,
                Name = db.Name,
                BaseUrl = db.BaseUrl,
                GlobalHeaders = globalHeaders,
                IsProduction = db.IsProduction,
                AllowExecution = db.AllowExecution,
                CreatedAt = db.CreatedAt,
                UpdatedAt = db.UpdatedAt,
                CreatedBy = db.CreatedBy,
                UpdatedBy = db.UpdatedBy
            };
        }

        private class EnvironmentDb
        {
            public Guid Id { get; set; }
            public Guid ProjectId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string BaseUrl { get; set; } = string.Empty;
            public string? GlobalHeaders { get; set; }
            public bool IsProduction { get; set; }
            public bool AllowExecution { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public Guid CreatedBy { get; set; }
            public Guid? UpdatedBy { get; set; }
        }
    }
}