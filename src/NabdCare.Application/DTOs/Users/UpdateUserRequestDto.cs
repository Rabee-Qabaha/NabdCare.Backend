using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Users;

[ExportTsClass]
public class UpdateUserRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid RoleId { get; set; }
}