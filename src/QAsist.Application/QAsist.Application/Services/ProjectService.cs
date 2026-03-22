using AutoMapper;
using QAsist.Application.Common.Exceptions;
using QAsist.Application.Common.Responses;
using QAsist.Application.DTOs;
using QAsist.Application.Interfaces.IRepositories;
using QAsist.Application.Interfaces.IServices;
using QAsist.Domain.Entities;

namespace QAsist.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IMapper _mapper;

        public ProjectService(IProjectRepository projectRepository, IMapper mapper)
        {
            _projectRepository = projectRepository;
            _mapper = mapper;
        }

        public async Task<ProjectDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var project = await _projectRepository.GetByIdAsync(id, cancellationToken);

            if (project == null)
                throw new NotFoundException(ResponseMessages.ProjectNotFound);

            return _mapper.Map<ProjectDto>(project);
        }

        public async Task<IEnumerable<ProjectDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var projects = await _projectRepository.GetAllAsync(cancellationToken);
            return _mapper.Map<IEnumerable<ProjectDto>>(projects);
        }

        public async Task<(IEnumerable<ProjectDto> Projects, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var (projects, totalCount) = await _projectRepository.GetPagedAsync(pageNumber, pageSize, cancellationToken);
            var projectDtos = _mapper.Map<IEnumerable<ProjectDto>>(projects);

            return (projectDtos, totalCount);
        }

        public async Task<ProjectDto> CreateAsync(CreateProjectDto dto, Guid userId, CancellationToken cancellationToken = default)
        {
            var codeExists = await _projectRepository.ExistsByCodeAsync(dto.Code, null, cancellationToken);

            if (codeExists)
                throw new ConflictException(ResponseMessages.ProjectCodeAlreadyExists);

            var project = _mapper.Map<Project>(dto);
            project.Id = Guid.NewGuid();

            var projectId = await _projectRepository.CreateAsync(project, userId, cancellationToken);
            project.Id = projectId;

            return _mapper.Map<ProjectDto>(project);
        }

        public async Task<ProjectDto> UpdateAsync(UpdateProjectDto dto, Guid userId, CancellationToken cancellationToken = default)
        {
            var existingProject = await _projectRepository.GetByIdAsync(dto.Id, cancellationToken);

            if (existingProject == null)
                throw new NotFoundException(ResponseMessages.ProjectNotFound);

            var codeExists = await _projectRepository.ExistsByCodeAsync(dto.Code, dto.Id, cancellationToken);

            if (codeExists)
                throw new ConflictException(ResponseMessages.ProjectCodeAlreadyExists);

            var project = _mapper.Map<Project>(dto);
            await _projectRepository.UpdateAsync(project, userId, cancellationToken);

            return _mapper.Map<ProjectDto>(project);
        }

        public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            var project = await _projectRepository.GetByIdAsync(id, cancellationToken);

            if (project == null)
                throw new NotFoundException(ResponseMessages.ProjectNotFound);

            await _projectRepository.DeleteAsync(id, userId, cancellationToken);
        }
    }
}
