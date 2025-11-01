// NabdCare.Application/Mappings/SubscriptionProfile.cs
using AutoMapper;
using NabdCare.Application.DTOs.Clinics.Subscriptions;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Mappings;

public class SubscriptionProfile : Profile
{
    public SubscriptionProfile()
    {
        CreateMap<CreateSubscriptionRequestDto, Subscription>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Clinic, o => o.Ignore())
            .ForMember(d => d.Payments, o => o.Ignore())
            .ForMember(d => d.PreviousSubscription, o => o.Ignore())
            .ForMember(d => d.PreviousSubscriptionId, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletedAt, o => o.Ignore())
            .ForMember(d => d.DeletedBy, o => o.Ignore());

        CreateMap<UpdateSubscriptionRequestDto, Subscription>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.ClinicId, o => o.Ignore())
            .ForMember(d => d.Clinic, o => o.Ignore())
            .ForMember(d => d.Payments, o => o.Ignore())
            .ForMember(d => d.PreviousSubscription, o => o.Ignore())
            .ForMember(d => d.PreviousSubscriptionId, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletedAt, o => o.Ignore())
            .ForMember(d => d.DeletedBy, o => o.Ignore());

        CreateMap<Subscription, SubscriptionResponseDto>();
    }
}