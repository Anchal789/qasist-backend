using Dapper;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Domain.Entities;
using QAsist.Infrastructure.Persistence;

namespace QAsist.Infrastructure.Repository
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ProjectRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new { p_id = id };
            return await connection.QueryFirstOrDefaultAsync<Project>(
                SqlQueries.Projects.GetById,
                parameters);
        }

        public async Task<IEnumerable<Project>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            return await connection.QueryAsync<Project>(SqlQueries.Projects.GetAll);
        }

        public async Task<(IEnumerable<Project> Projects, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new
            {
                p_page_number = pageNumber,
                p_page_size = pageSize
            };

            var projects = await connection.QueryAsync<Project>(
                SqlQueries.Projects.GetPaged,
                parameters);

            var totalCount = await connection.ExecuteScalarAsync<int>(
                SqlQueries.Projects.GetPagedCount);

            return (projects, totalCount);
        }

        //public async Task<Guid> CreateAsync(Project project, Guid userId, CancellationToken cancellationToken = default)
        //{
        //    using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        //    var parameters = new
        //    {
        //        p_id = project.Id,
        //        p_name = project.Name,
        //        p_description = project.Description,
        //        p_code = project.Code,
        //        p_owner_id = project.OwnerId,
        //        p_start_date = project.StartDate,
        //        p_end_date = project.EndDate,
        //        p_status = project.Status,
        //        p_created_by = userId
        //    };

        //    return await connection.ExecuteScalarAsync<Guid>(
        //        SqlQueries.Projects.Create,
        //        parameters);
        //}
        public async Task<Guid> CreateAsync(Project project, Guid userId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new
            {
                p_id = project.Id,
                p_name = project.Name,
                p_description = project.Description,
                p_code = project.Code,
                p_owner_id = project.OwnerId,
                p_start_date = project.StartDate,
                p_end_date = project.EndDate,
                p_status = (int)project.Status, // IMPORTANT
                p_created_by = userId
            };

            const string sql = @"
        INSERT INTO projects (
            id, name, description, code, owner_id,
            start_date, end_date, status, created_by, created_at
        )
        VALUES (
            @p_id, @p_name, @p_description, @p_code, @p_owner_id,
            @p_start_date, @p_end_date, @p_status, @p_created_by, NOW()
        )
        RETURNING id;
    ";

            return await connection.ExecuteScalarAsync<Guid>(sql, parameters);
        }
        public async Task<bool> UpdateAsync(Project project, Guid userId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new
            {
                p_id = project.Id,
                p_name = project.Name,
                p_description = project.Description,
                p_code = project.Code,
                p_owner_id = project.OwnerId,
                p_start_date = project.StartDate,
                p_end_date = project.EndDate,
                p_status = project.Status,
                p_updated_by = userId
            };

            var result = await connection.ExecuteScalarAsync<bool>(
                SqlQueries.Projects.Update,
                parameters);

            return result;
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new
            {
                p_id = id,
                p_deleted_by = userId
            };

            var result = await connection.ExecuteScalarAsync<bool>(
                SqlQueries.Projects.Delete,
                parameters);

            return result;
        }

        public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var parameters = new
            {
                p_code = code,
                p_exclude_id = excludeId
            };

            return await connection.ExecuteScalarAsync<bool>(
                SqlQueries.Projects.ExistsByCode,
                parameters);
        }
    }
}
