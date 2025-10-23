using AutoMapper;
using NabdCare.Application.DTOs.Roles;
using NabdCare.Domain.Entities.Permissions;

namespace NabdCare.Application.Mappings;

public class RoleProfile : Profile
{
    public RoleProfile()
    {
        // Role -> RoleResponseDto
        CreateMap<Role, RoleResponseDto>()
            .ForMember(dest => dest.ClinicName, opt => opt.MapFrom(src => src.Clinic != null ? src.Clinic.Name : null))
            .ForMember(dest => dest.UserCount, opt => opt.Ignore()) // Set manually in service
            .ForMember(dest => dest.PermissionCount, opt => opt.Ignore()); // Set manually in service

        // CreateRoleRequestDto -> Role
        CreateMap<CreateRoleRequestDto, Role>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsSystemRole, opt => opt.MapFrom(src => false)) // Only seeders create system roles
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Clinic, opt => opt.Ignore())
            .ForMember(dest => dest.Users, opt => opt.Ignore())
            .ForMember(dest => dest.RolePermissions, opt => opt.Ignore());

        // UpdateRoleRequestDto -> Role
        CreateMap<UpdateRoleRequestDto, Role>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsSystemRole, opt => opt.Ignore()) // Cannot change
            .ForMember(dest => dest.ClinicId, opt => opt.Ignore()) // Cannot change
            .ForMember(dest => dest.TemplateRoleId, opt => opt.Ignore()) // Cannot change
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Clinic, opt => opt.Ignore())
            .ForMember(dest => dest.Users, opt => opt.Ignore())
            .ForMember(dest => dest.RolePermissions, opt => opt.Ignore());
    }
}