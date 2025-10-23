using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Clinics;

/// <summary>
/// DTO for updating clinic subscription status only
/// </summary>
[ExportTsClass]
public class UpdateClinicStatusDto
{
    public SubscriptionStatus Status { get; set; }
}