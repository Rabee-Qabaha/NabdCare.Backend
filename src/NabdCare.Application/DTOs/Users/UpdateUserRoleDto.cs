using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Users;

[ExportTsClass]
public class UpdateUserRoleDto
{
    public Guid RoleId { get; set; } 
}