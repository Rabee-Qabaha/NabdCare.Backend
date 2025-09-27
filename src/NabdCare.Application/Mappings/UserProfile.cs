using AutoMapper;
using NabdCare.Application.DTOs.Users;
using NabdCare.Domain.Entities.Users;

namespace NabdCare.Application.mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        // Domain → DTO
        CreateMap<User, UserResponseDto>()
            .ForMember(dest => dest.ClinicName,
                opt => opt.MapFrom(src => src.Clinic != null ? src.Clinic.Name : null));

        // DTO → Domain
        CreateMap<CreateUserRequestDto, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // handled in service
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

        CreateMap<UpdateUserRequestDto, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ClinicId, opt => opt.Ignore()); // clinic set via TenantContext
    }
}