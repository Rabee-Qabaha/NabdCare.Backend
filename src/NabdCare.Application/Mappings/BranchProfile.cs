using AutoMapper;
using NabdCare.Application.DTOs.Clinics.Branches;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Application.Mappings;

public class BranchProfile : Profile
{
    public BranchProfile()
    {
        // ==========================================
        // CREATE MAP
        // ==========================================
        CreateMap<CreateBranchRequestDto, Branch>()
            .ForMember(d => d.Id, o => o.Ignore())
            // ClinicId is mapped automatically
            
            // IsActive defaults to true in Service logic, so ignore DTO input if any
            .ForMember(d => d.IsActive, o => o.Ignore()) 
            
            // Audit fields
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletedAt, o => o.Ignore())
            .ForMember(d => d.DeletedBy, o => o.Ignore());

        // ==========================================
        // UPDATE MAP
        // ==========================================
        CreateMap<UpdateBranchRequestDto, Branch>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.ClinicId, o => o.Ignore())
            .ForMember(d => d.Clinic, o => o.Ignore())
            
            // Map explicit fields
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.Address, o => o.MapFrom(s => s.Address))
            .ForMember(d => d.Phone, o => o.MapFrom(s => s.Phone))
            .ForMember(d => d.Email, o => o.MapFrom(s => s.Email))
            .ForMember(d => d.IsMain, o => o.MapFrom(s => s.IsMain))
            .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive))

            // Ignore Audit
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.Ignore())
            .ForMember(d => d.IsDeleted, o => o.Ignore())
            .ForMember(d => d.DeletedAt, o => o.Ignore())
            .ForMember(d => d.DeletedBy, o => o.Ignore());

        // ==========================================
        // RESPONSE MAP
        // ==========================================
        CreateMap<Branch, BranchResponseDto>();
    }
}