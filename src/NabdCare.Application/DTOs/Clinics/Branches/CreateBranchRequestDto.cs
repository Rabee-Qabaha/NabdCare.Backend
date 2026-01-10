using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Clinics.Branches;

[ExportTsClass]
public class CreateBranchRequestDto
{
    public Guid ClinicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsMain { get; set; } = false;
}