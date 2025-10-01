using AutoMapper;
using NabdCare.Application.DTOs.Clinics.Subscriptions;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Mappings;
public class SubscriptionProfile : Profile
{
    public SubscriptionProfile()
    {
        CreateMap<CreateSubscriptionRequestDto, Subscription>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Payments, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<UpdateSubscriptionRequestDto, Subscription>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Payments, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<Subscription, SubscriptionResponseDto>();
    }
}