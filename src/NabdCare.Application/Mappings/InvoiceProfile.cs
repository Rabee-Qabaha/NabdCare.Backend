using AutoMapper;
using NabdCare.Application.DTOs.Invoices;
using NabdCare.Domain.Entities.Invoices;

namespace NabdCare.Application.Mappings;

public class InvoiceProfile : Profile
{
    public InvoiceProfile()
    {
        CreateMap<Invoice, InvoiceDto>()
            .ForMember(d => d.BalanceDue, o => o.MapFrom(s => s.BalanceDue));

        CreateMap<InvoiceItem, InvoiceItemDto>();
    }
}