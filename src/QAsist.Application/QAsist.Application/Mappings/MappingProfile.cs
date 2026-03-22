using AutoMapper;
using QAsist.Application.DTOs;
using QAsist.Domain.Entities;

namespace QAsist.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Project, ProjectDto>();
            CreateMap<CreateProjectDto, Project>();
            CreateMap<UpdateProjectDto, Project>();

            CreateMap<User, UserDto>();
            CreateMap<CreateUserDto, User>();
        }
    }
}
