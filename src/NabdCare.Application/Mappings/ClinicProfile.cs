using AutoMapper;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Domain.Entities.Clinic;

namespace NabdCare.Application.mappings;

public class ClinicProfile : Profile
{
    public ClinicProfile()
    {
        CreateMap<CreateClinicRequestDto, Clinic>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<UpdateClinicRequestDto, Clinic>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<Clinic, ClinicResponseDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.SubscriptionType, opt => opt.MapFrom(s => s.SubscriptionType.ToString()));
    }
}