using AutoMapper;
using NabdCare.Application.DTOs.Payments;
using NabdCare.Domain.Entities.Payments;

namespace NabdCare.Application.Mappings;

public class PaymentProfile : Profile
{
    public PaymentProfile()
    {
        // Payment -> PaymentDto
        CreateMap<Payment, PaymentDto>()
            .ForMember(dest => dest.Allocations, opt => opt.MapFrom(src => src.Allocations))
            .ForMember(dest => dest.ChequeDetail, opt => opt.MapFrom(src => src.ChequeDetail));

        // PaymentAllocation -> PaymentAllocationDto
        CreateMap<PaymentAllocation, PaymentAllocationDto>()
            .ForMember(dest => dest.InvoiceNumber, opt => opt.MapFrom(src => src.Invoice.InvoiceNumber))
            // ✅ Map enriched fields from Parent Payment
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.Payment.Method))
            .ForMember(dest => dest.PaymentDate, opt => opt.MapFrom(src => src.Payment.PaymentDate))
            .ForMember(dest => dest.Reference, opt => opt.MapFrom(src => 
                src.Payment.Method == Domain.Enums.PaymentMethod.Cheque 
                    ? src.Payment.ChequeDetail != null ? src.Payment.ChequeDetail.ChequeNumber : "Cheque"
                    : src.Payment.TransactionId ?? "Cash"));

        // ChequePaymentDetail -> ChequePaymentDetailDto
        CreateMap<ChequePaymentDetail, ChequePaymentDetailDto>();

        // CreatePaymentRequestDto -> Payment
        CreateMap<CreatePaymentRequestDto, Payment>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Domain.Enums.PaymentStatus.Pending))
            .ForMember(dest => dest.Allocations, opt => opt.Ignore()) // Handled manually in service
            .ForMember(dest => dest.ChequeDetail, opt => opt.Ignore()); // Handled manually in service

        // CreateChequeDetailDto -> ChequePaymentDetail
        CreateMap<CreateChequeDetailDto, ChequePaymentDetail>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Domain.Enums.ChequeStatus.Pending));
    }
}