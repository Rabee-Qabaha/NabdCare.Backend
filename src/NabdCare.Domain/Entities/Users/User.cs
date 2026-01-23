using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NabdCare.Domain.Entities.Clinics;
using NabdCare.Domain.Entities.Permissions;
using NabdCare.Domain.Entities.Roles;
using NabdCare.Domain.Entities.Scheduling;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Entities.Users;

[ExportTsClass]
[TsIgnoreBase]
public class User : BaseEntity
{
    public Guid? ClinicId { get; set; }
    [ForeignKey(nameof(ClinicId))]
    public Clinic? Clinic { get; set; }

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public Guid RoleId { get; set; }
    [ForeignKey(nameof(RoleId))]
    public Role Role { get; set; } = null!;

    [Required]
    public bool IsActive { get; set; } = true;

    public Guid? CreatedByUserId { get; set; }
    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    public ICollection<UserPermission> Permissions { get; set; } = new List<UserPermission>();
    public ICollection<PractitionerSchedule> Schedules { get; set; } = new List<PractitionerSchedule>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}