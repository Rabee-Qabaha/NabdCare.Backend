using AutoMapper;
using NabdCare.Application.DTOs.Permissions;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Application.mappings;

public class PermissionProfile : Profile
{
    public PermissionProfile()
    {
        CreateMap<AppPermission, PermissionResponseDto>();
        CreateMap<CreatePermissionDto, AppPermission>();
        CreateMap<UpdatePermissionDto, AppPermission>();
    }
}