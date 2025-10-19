using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Permissions;

[ExportTsClass]
public class PermissionResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
}