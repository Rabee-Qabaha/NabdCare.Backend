using AutoMapper;
using NabdCare.Application.DTOs.Clinics;
using NabdCare.Application.DTOs.Subscriptions;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Subscriptions;
using NabdCare.Domain.Enums;

namespace NabdCare.Application.Mappings;

/// <summary>
/// AutoMapper profile for Clinic and Subscription entities.
/// Updated for Separated Architecture (Identity First).
/// </summary>
public class ClinicProfile : Profile
{
    public ClinicProfile()
    {
        // ============================================
        // VALUE OBJECT MAPPINGS
        // ============================================
        CreateMap<ClinicSettings, ClinicSettingsDto>().ReverseMap();

        // ============================================
        // CLINIC MAPPINGS
        // ============================================

        // DTO -> Entity (Create)
        CreateMap<CreateClinicRequestDto, Clinic>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            
            // ✅ Status is set to 'Inactive' by the Service, not the DTO
            .ForMember(d => d.Status, opt => opt.Ignore()) 
            
            // ✅ Subscriptions are created via separate API, not here
            .ForMember(d => d.Subscriptions, opt => opt.Ignore())
            
            .ForMember(d => d.Settings, opt => opt.MapFrom(src => src.Settings ?? new ClinicSettingsDto()))
            
            // Audit & System Fields
            .ForMember(d => d.BranchCount, opt => opt.Ignore()) // Defaults to 1 or set by service
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
            .ForMember(d => d.Status, opt => opt.Ignore()) 
            .ForMember(d => d.Subscriptions, opt => opt.Ignore())
            .ForMember(d => d.Settings, opt => opt.MapFrom(src => src.Settings))
            .ForMember(d => d.BranchCount, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.CreatedBy, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedBy, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore())
            .ForMember(d => d.DeletedAt, opt => opt.Ignore())
            .ForMember(d => d.DeletedBy, opt => opt.Ignore());

        // Entity -> Response DTO
        CreateMap<Clinic, ClinicResponseDto>()
            .ForMember(dest => dest.Settings, opt => opt.MapFrom(src => src.Settings))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            
            // ==================================================================================
            // ⚠️ FLATTENING LOGIC: Extract data from the latest active subscription (if any)
            // ==================================================================================
            
            .ForMember(dest => dest.SubscriptionType, opt => opt.MapFrom(src =>
                src.Subscriptions != null && src.Subscriptions.Any(s => !s.IsDeleted)
                    ? src.Subscriptions.Where(s => !s.IsDeleted).OrderByDescending(s => s.StartDate).First().Type
                    : (SubscriptionType?)null)) // ✅ Null if no subscription
            
            .ForMember(dest => dest.SubscriptionStartDate, opt => opt.MapFrom(src =>
                src.Subscriptions != null && src.Subscriptions.Any(s => !s.IsDeleted)
                    ? src.Subscriptions.Where(s => !s.IsDeleted).OrderByDescending(s => s.StartDate).First().StartDate
                    : (DateTime?)null))
            
            .ForMember(dest => dest.SubscriptionEndDate, opt => opt.MapFrom(src =>
                src.Subscriptions != null && src.Subscriptions.Any(s => !s.IsDeleted)
                    ? src.Subscriptions.Where(s => !s.IsDeleted).OrderByDescending(s => s.StartDate).First().EndDate
                    : (DateTime?)null))
            
            .ForMember(dest => dest.SubscriptionFee, opt => opt.MapFrom(src =>
                src.Subscriptions != null && src.Subscriptions.Any(s => !s.IsDeleted)
                    ? src.Subscriptions.Where(s => !s.IsDeleted).OrderByDescending(s => s.StartDate).First().Fee
                    : (decimal?)null));

        // ============================================
        // SUBSCRIPTION MAPPINGS
        // ============================================

        // DTO -> Entity (Create)
        CreateMap<CreateSubscriptionRequestDto, Subscription>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Clinic, opt => opt.Ignore())
            .ForMember(d => d.Payments, opt => opt.Ignore())
            .ForMember(d => d.Invoices, opt => opt.Ignore())
            // Computed fields ignored (set in service)
            .ForMember(d => d.Fee, opt => opt.Ignore())
            .ForMember(d => d.Status, opt => opt.Ignore())
            .ForMember(d => d.StartDate, opt => opt.Ignore())
            .ForMember(d => d.EndDate, opt => opt.Ignore())
            
            // Audit
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
            .ForMember(d => d.Invoices, opt => opt.Ignore())
            .ForMember(d => d.Fee, opt => opt.Ignore()) // Recalculated in service
            
            // Audit
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