using AutoMapper;
using NabdCare.Application.DTOs.Clinics.Subscriptions;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Mappings;

/// <summary>
/// AutoMapper profile for Subscription entity mappings.
/// Author: Rabee-Qabaha
/// Updated: 2025-10-22 20:48:34 UTC
/// </summary>
public class SubscriptionProfile : Profile
{
    public SubscriptionProfile()
    {
        // ============================================
        // CREATE SUBSCRIPTION (DTO -> Entity)
        // ============================================
        CreateMap<CreateSubscriptionRequestDto, Subscription>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Clinic, opt => opt.Ignore())
            .ForMember(dest => dest.Payments, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore());

        // ============================================
        // UPDATE SUBSCRIPTION (DTO -> Entity)
        // ============================================
        CreateMap<UpdateSubscriptionRequestDto, Subscription>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ClinicId, opt => opt.Ignore())
            .ForMember(dest => dest.Clinic, opt => opt.Ignore())
            .ForMember(dest => dest.Payments, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore());

        // ============================================
        // SUBSCRIPTION RESPONSE (Entity -> DTO)
        // ============================================
        CreateMap<Subscription, SubscriptionResponseDto>();
    }
}