using AutoMapper;
using NabdCare.Application.DTOs.Invoices;
using NabdCare.Domain.Entities.Invoices;

namespace NabdCare.Application.Mappings;

public class InvoiceProfile : Profile
{
    public InvoiceProfile()
    {
        CreateMap<Invoice, InvoiceDto>()
            .ForMember(d => d.BalanceDue, o => o.MapFrom(s => s.BalanceDue))
            .ForMember(d => d.Payments, o => o.MapFrom(s => s.PaymentAllocations)); // Map Allocations to "Payments" list in DTO

        CreateMap<InvoiceItem, InvoiceItemDto>();
    }
}