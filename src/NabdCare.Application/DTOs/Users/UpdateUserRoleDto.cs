using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Users;

[ExportTsClass]
public class UpdateUserRoleDto
{
    public UserRole Role { get; set; }
}