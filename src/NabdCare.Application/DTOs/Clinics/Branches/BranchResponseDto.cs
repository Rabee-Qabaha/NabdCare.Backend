using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Clinics.Branches;

[ExportTsClass]
public class BranchResponseDto
{
    public Guid Id { get; set; }
    public Guid ClinicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public bool IsMain { get; set; }
}