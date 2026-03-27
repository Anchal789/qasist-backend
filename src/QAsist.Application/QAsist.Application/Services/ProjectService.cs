using AutoMapper;
using QAsist.Application.Common.Exceptions;
using QAsist.Application.Common.Responses;
using QAsist.Application.DTOs;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Application.Interfaces.IServices;
using QAsist.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace QAsist.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ProjectService> _logger;
        public ProjectService(
            IProjectRepository projectRepository,
            IMapper mapper,
            ILogger<ProjectService> logger)
        {
            _projectRepository = projectRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ProjectDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching project with Id: {ProjectId}", id);

            var project = await _projectRepository.GetByIdAsync(id, cancellationToken);

            if (project == null)
            {
                _logger.LogWarning("Project not found with Id: {ProjectId}", id);
                throw new NotFoundException(ResponseMessages.ProjectNotFound);
            }

            _logger.LogInformation("Project retrieved successfully with Id: {ProjectId}", id);

            return _mapper.Map<ProjectDto>(project);
        }

        public async Task<IEnumerable<ProjectDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching all projects");

            var projects = await _projectRepository.GetAllAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} projects", projects.Count());

            return _mapper.Map<IEnumerable<ProjectDto>>(projects);
        }

        public async Task<(IEnumerable<ProjectDto> Projects, int TotalCount)> GetPagedAsync(
     int pageNumber,
     int pageSize,
     CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching paged projects: PageNumber={PageNumber}, PageSize={PageSize}", pageNumber, pageSize);

            var (projects, totalCount) = await _projectRepository.GetPagedAsync(pageNumber, pageSize, cancellationToken);

            _logger.LogInformation("Retrieved {Count} projects out of {TotalCount}", projects.Count(), totalCount);

            var projectDtos = _mapper.Map<IEnumerable<ProjectDto>>(projects);

            return (projectDtos, totalCount);
        }

        public async Task<ProjectDto> CreateAsync(CreateProjectDto dto, Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating project with Code: {Code} by User: {UserId}", dto.Code, userId);

            var codeExists = await _projectRepository.ExistsByCodeAsync(dto.Code, null, cancellationToken);

            if (codeExists)
            {
                _logger.LogWarning("Project creation failed. Code already exists: {Code}", dto.Code);
                throw new ConflictException(ResponseMessages.ProjectCodeAlreadyExists);
            }

            var project = _mapper.Map<Project>(dto);
            project.Id = Guid.NewGuid();

            var projectId = await _projectRepository.CreateAsync(project, userId, cancellationToken);

            _logger.LogInformation("Project created successfully with Id: {ProjectId}", projectId);

            project.Id = projectId;

            return _mapper.Map<ProjectDto>(project);
        }

        public async Task<ProjectDto> UpdateAsync(UpdateProjectDto dto, Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating project with Id: {ProjectId} by User: {UserId}", dto.Id, userId);

            var existingProject = await _projectRepository.GetByIdAsync(dto.Id, cancellationToken);

            if (existingProject == null)
            {
                _logger.LogWarning("Update failed. Project not found: {ProjectId}", dto.Id);
                throw new NotFoundException(ResponseMessages.ProjectNotFound);
            }

            var codeExists = await _projectRepository.ExistsByCodeAsync(dto.Code, dto.Id, cancellationToken);

            if (codeExists)
            {
                _logger.LogWarning("Update failed. Code already exists: {Code}", dto.Code);
                throw new ConflictException(ResponseMessages.ProjectCodeAlreadyExists);
            }

            var project = _mapper.Map<Project>(dto);

            await _projectRepository.UpdateAsync(project, userId, cancellationToken);

            _logger.LogInformation("Project updated successfully with Id: {ProjectId}", dto.Id);

            return _mapper.Map<ProjectDto>(project);
        }

        public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting project with Id: {ProjectId} by User: {UserId}", id, userId);

            var project = await _projectRepository.GetByIdAsync(id, cancellationToken);

            if (project == null)
            {
                _logger.LogWarning("Delete failed. Project not found: {ProjectId}", id);
                throw new NotFoundException(ResponseMessages.ProjectNotFound);
            }

            await _projectRepository.DeleteAsync(id, userId, cancellationToken);

            _logger.LogInformation("Project deleted successfully with Id: {ProjectId}", id);
        }
    }
}
