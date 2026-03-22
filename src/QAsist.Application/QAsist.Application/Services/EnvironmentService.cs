using Microsoft.Extensions.Logging;
using QAsist.Application.Common.Exceptions;
using QAsist.Application.DTOs;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Application.Interfaces.IServices;
using QAsist.Domain.Entities;

namespace QAsist.Application.Services
{
    public class EnvironmentService : IEnvironmentService
    {
        private readonly IEnvironmentRepository _environmentRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly ILogger<EnvironmentService> _logger;

        public EnvironmentService(
            IEnvironmentRepository environmentRepository,
            IProjectRepository projectRepository,
            ILogger<EnvironmentService> logger)
        {
            _environmentRepository = environmentRepository;
            _projectRepository = projectRepository;
            _logger = logger;
        }

        public async Task<EnvironmentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var environment = await _environmentRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Environment with ID {id} not found");

            return MapToDto(environment);
        }

        public async Task<IEnumerable<EnvironmentDto>> GetByProjectAsync(
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            var environments = await _environmentRepository.GetByProjectAsync(projectId, cancellationToken);
            return environments.Select(MapToDto);
        }

        public async Task<EnvironmentDto> CreateAsync(
            CreateEnvironmentDto dto,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            // Validate project exists
            var project = await _projectRepository.GetByIdAsync(dto.ProjectId, cancellationToken)
                ?? throw new NotFoundException($"Project with ID {dto.ProjectId} not found");

            // Validate unique name per project
            var existing = await _environmentRepository.GetByNameAsync(dto.ProjectId, dto.Name, cancellationToken);
            if (existing != null)
            {
                throw new ValidationException($"Environment '{dto.Name}' already exists for this project");
            }

            // Validate base URL
            if (!Uri.TryCreate(dto.BaseUrl, UriKind.Absolute, out _))
            {
                throw new ValidationException("Invalid base URL format");
            }

            var environment = new Domain.Entities.Environment
            {
                Id = Guid.NewGuid(),
                ProjectId = dto.ProjectId,
                Name = dto.Name,
                BaseUrl = dto.BaseUrl.TrimEnd('/'),
                GlobalHeaders = dto.GlobalHeaders ?? new Dictionary<string, string>(),
                IsProduction = dto.IsProduction,
                AllowExecution = !dto.IsProduction, // Production disabled by default
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _environmentRepository.CreateAsync(environment, userId, cancellationToken);

            _logger.LogInformation(
                "Environment '{Name}' created for Project {ProjectId} by User {UserId}",
                environment.Name,
                environment.ProjectId,
                userId);

            return MapToDto(environment);
        }

        public async Task<EnvironmentDto> UpdateAsync(
            UpdateEnvironmentDto dto,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var environment = await _environmentRepository.GetByIdAsync(dto.Id, cancellationToken)
                ?? throw new NotFoundException($"Environment with ID {dto.Id} not found");

            // Validate base URL
            if (!Uri.TryCreate(dto.BaseUrl, UriKind.Absolute, out _))
            {
                throw new ValidationException("Invalid base URL format");
            }

            // Check name uniqueness if changed
            if (environment.Name != dto.Name)
            {
                var existing = await _environmentRepository.GetByNameAsync(
                    environment.ProjectId,
                    dto.Name,
                    cancellationToken);

                if (existing != null && existing.Id != dto.Id)
                {
                    throw new ValidationException($"Environment '{dto.Name}' already exists for this project");
                }
            }

            environment.Name = dto.Name;
            environment.BaseUrl = dto.BaseUrl.TrimEnd('/');
            environment.GlobalHeaders = dto.GlobalHeaders ?? new Dictionary<string, string>();
            environment.IsProduction = dto.IsProduction;
            environment.AllowExecution = dto.AllowExecution;
            environment.UpdatedBy = userId;
            environment.UpdatedAt = DateTime.UtcNow;

            await _environmentRepository.UpdateAsync(environment, userId, cancellationToken);

            _logger.LogInformation(
                "Environment {EnvironmentId} updated by User {UserId}",
                environment.Id,
                userId);

            return MapToDto(environment);
        }

        public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            var environment = await _environmentRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Environment with ID {id} not found");

            await _environmentRepository.DeleteAsync(id, userId, cancellationToken);

            _logger.LogInformation(
                "Environment {EnvironmentId} deleted by User {UserId}",
                id,
                userId);
        }

        public async Task<EnvironmentDto?> GetByNameAsync(
            Guid projectId,
            string environmentName,
            CancellationToken cancellationToken = default)
        {
            var environment = await _environmentRepository.GetByNameAsync(
                projectId,
                environmentName,
                cancellationToken);

            return environment != null ? MapToDto(environment) : null;
        }

        private static EnvironmentDto MapToDto(Domain.Entities.Environment entity)
        {
            return new EnvironmentDto
            {
                Id = entity.Id,
                ProjectId = entity.ProjectId,
                Name = entity.Name,
                BaseUrl = entity.BaseUrl,
                GlobalHeaders = entity.GlobalHeaders,
                IsProduction = entity.IsProduction,
                AllowExecution = entity.AllowExecution,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}