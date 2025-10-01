using AutoMapper;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Enums;
using System.Linq;

namespace NabdCare.Application.Mappings
{
    public class ClinicProfile : Profile
    {
        public ClinicProfile()
        {
            // Map DTO -> Entity
            CreateMap<CreateClinicRequestDto, Clinic>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.IsDeleted, opt => opt.Ignore())
                .ForMember(d => d.Subscriptions, opt => opt.Ignore()); // handled separately

            CreateMap<UpdateClinicRequestDto, Clinic>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.IsDeleted, opt => opt.Ignore())
                .ForMember(d => d.Subscriptions, opt => opt.Ignore()); // handled separately

            // Map Entity -> DTO
            CreateMap<Clinic, ClinicResponseDto>()
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.SubscriptionType, opt => opt.MapFrom(s =>
                    s.Subscriptions != null && s.Subscriptions.Any()
                        ? s.Subscriptions.OrderByDescending(sub => sub.StartDate).First().Type.ToString()
                        : string.Empty
                ))
                .ForMember(d => d.SubscriptionStartDate, opt => opt.MapFrom(s =>
                    s.Subscriptions != null && s.Subscriptions.Any()
                        ? s.Subscriptions.OrderByDescending(sub => sub.StartDate).First().StartDate
                        : default(DateTime)
                ))
                .ForMember(d => d.SubscriptionEndDate, opt => opt.MapFrom(s =>
                    s.Subscriptions != null && s.Subscriptions.Any()
                        ? s.Subscriptions.OrderByDescending(sub => sub.StartDate).First().EndDate
                        : default(DateTime)
                ));
        }
    }
}
