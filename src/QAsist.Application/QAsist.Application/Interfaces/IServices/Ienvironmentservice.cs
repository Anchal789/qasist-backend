using QAsist.Application.DTOs;

namespace QAsist.Application.Interfaces.IServices
{
    /// <summary>
    /// Environment configuration management
    /// </summary>
    public interface IEnvironmentService
    {
        Task<EnvironmentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IEnumerable<EnvironmentDto>> GetByProjectAsync(
            Guid projectId,
            CancellationToken cancellationToken = default);

        Task<EnvironmentDto> CreateAsync(
            CreateEnvironmentDto dto,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<EnvironmentDto> UpdateAsync(
            UpdateEnvironmentDto dto,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

        Task<EnvironmentDto?> GetByNameAsync(
            Guid projectId,
            string environmentName,
            CancellationToken cancellationToken = default);
    }
}
