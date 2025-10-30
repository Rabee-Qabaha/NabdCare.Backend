using AutoMapper;
using NabdCare.Domain.Entities.Audits;
using NabdCare.Application.DTOs.AuditLogs;

namespace NabdCare.Application.Mappings;

public class AuditLogMappingProfile : Profile
{
    public AuditLogMappingProfile()
    {
        // Map AuditLog -> AuditLogResponseDto
        CreateMap<AuditLog, AuditLogResponseDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.UserEmail))
            .ForMember(dest => dest.ClinicId, opt => opt.MapFrom(src => src.ClinicId))
            .ForMember(dest => dest.EntityType, opt => opt.MapFrom(src => src.EntityType))
            .ForMember(dest => dest.EntityId, opt => opt.MapFrom(src => src.EntityId))
            .ForMember(dest => dest.Action, opt => opt.MapFrom(src => src.Action))
            .ForMember(dest => dest.Changes, opt => opt.MapFrom(src => src.Changes))
            .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.Reason))
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp))
            .ForMember(dest => dest.IpAddress, opt => opt.MapFrom(src => src.IpAddress))
            .ForMember(dest => dest.UserAgent, opt => opt.MapFrom(src => src.UserAgent));
    }
}