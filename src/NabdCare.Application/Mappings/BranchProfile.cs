using AutoMapper;
using NabdCare.Application.DTOs.Clinics.Branches;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Mappings;

public class BranchProfile : Profile
{
    public BranchProfile()
    {
        CreateMap<CreateBranchRequestDto, Branch>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Clinic, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore());

        CreateMap<Branch, BranchResponseDto>();
    }
}