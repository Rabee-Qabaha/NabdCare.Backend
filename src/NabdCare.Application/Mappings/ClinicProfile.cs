using AutoMapper;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.DTOs.Clinics.Subscriptions;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Mappings;

/// <summary>
/// AutoMapper profile for Clinic and Subscription entities.
/// Updated: 2025-10-22 by Rabee-Qabaha
/// </summary>
public class ClinicProfile : Profile
{
    public ClinicProfile()
    {
        // ============================================
        // CLINIC MAPPINGS
        // ============================================

        // DTO -> Entity (Create)
        CreateMap<CreateClinicRequestDto, Clinic>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Subscriptions, opt => opt.Ignore()) // Created separately in service
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.CreatedBy, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedBy, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore())
            .ForMember(d => d.DeletedAt, opt => opt.Ignore())
            .ForMember(d => d.DeletedBy, opt => opt.Ignore());

        // DTO -> Entity (Update)
        CreateMap<UpdateClinicRequestDto, Clinic>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Subscriptions, opt => opt.Ignore()) // Handled separately in service
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.CreatedBy, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedBy, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore())
            .ForMember(d => d.DeletedAt, opt => opt.Ignore())
            .ForMember(d => d.DeletedBy, opt => opt.Ignore());

        // Entity -> Response DTO
        CreateMap<Clinic, ClinicResponseDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.SubscriptionType, opt => opt.MapFrom(src =>
                src.Subscriptions != null && src.Subscriptions.Any(s => !s.IsDeleted)
                    ? src.Subscriptions
                        .Where(s => !s.IsDeleted)
                        .OrderByDescending(s => s.StartDate)
                        .First().Type
                    : default))
            .ForMember(dest => dest.SubscriptionStartDate, opt => opt.MapFrom(src =>
                src.Subscriptions != null && src.Subscriptions.Any(s => !s.IsDeleted)
                    ? src.Subscriptions
                        .Where(s => !s.IsDeleted)
                        .OrderByDescending(s => s.StartDate)
                        .First().StartDate
                    : default(DateTime)))
            .ForMember(dest => dest.SubscriptionEndDate, opt => opt.MapFrom(src =>
                src.Subscriptions != null && src.Subscriptions.Any(s => !s.IsDeleted)
                    ? src.Subscriptions
                        .Where(s => !s.IsDeleted)
                        .OrderByDescending(s => s.StartDate)
                        .First().EndDate
                    : default(DateTime)))
            .ForMember(dest => dest.SubscriptionFee, opt => opt.MapFrom(src =>
                src.Subscriptions != null && src.Subscriptions.Any(s => !s.IsDeleted)
                    ? src.Subscriptions
                        .Where(s => !s.IsDeleted)
                        .OrderByDescending(s => s.StartDate)
                        .First().Fee
                    : 0m));

        // ============================================
        // SUBSCRIPTION MAPPINGS
        // ============================================

        // DTO -> Entity (Create)
        CreateMap<CreateSubscriptionRequestDto, Subscription>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Clinic, opt => opt.Ignore())
            .ForMember(d => d.Payments, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.CreatedBy, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedBy, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore())
            .ForMember(d => d.DeletedAt, opt => opt.Ignore())
            .ForMember(d => d.DeletedBy, opt => opt.Ignore());

        // DTO -> Entity (Update)
        CreateMap<UpdateSubscriptionRequestDto, Subscription>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.ClinicId, opt => opt.Ignore())
            .ForMember(d => d.Clinic, opt => opt.Ignore())
            .ForMember(d => d.Payments, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.CreatedBy, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedBy, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore())
            .ForMember(d => d.DeletedAt, opt => opt.Ignore())
            .ForMember(d => d.DeletedBy, opt => opt.Ignore());

        // Entity -> Response DTO
        CreateMap<Subscription, SubscriptionResponseDto>();
    }
}