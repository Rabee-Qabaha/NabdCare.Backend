using AutoMapper;
using NabdCare.Application.DTOs.Users;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Application.Mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        // User -> UserResponseDto
        CreateMap<User, UserResponseDto>()
            .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.RoleId))
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.Name))
            .ForMember(dest => dest.RoleColorCode, opt => opt.MapFrom(src => src.Role.ColorCode))
            .ForMember(dest => dest.RoleIcon, opt => opt.MapFrom(src => src.Role.IconClass))
            .ForMember(dest => dest.IsSystemRole, opt => opt.MapFrom(src => src.Role.IsSystemRole))
            .ForMember(dest => dest.ClinicName, opt => opt.MapFrom(src => src.Clinic != null ? src.Clinic.Name : null))
            .ForMember(dest => dest.CreatedByUserName, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FullName : null))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.IsDeleted));
        
        // CreateUserRequestDto -> User
        CreateMap<CreateUserRequestDto, User>()
            .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.RoleId))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore()); // System managed

        // UpdateUserRequestDto -> User
        CreateMap<UpdateUserRequestDto, User>()
            .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.RoleId))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.ClinicId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore());
    }
}