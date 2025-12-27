using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.Validator.Clinics.Branches;

[ExportTsClass]
public class UpdateBranchRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public bool IsMain { get; set; }
}