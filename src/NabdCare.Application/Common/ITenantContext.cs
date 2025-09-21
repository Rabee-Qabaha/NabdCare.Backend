namespace NabdCare.Application.Common;

public interface ITenantContext
{
    Guid? ClinicId { get; set; }
    bool IsSuperAdmin { get; set; }
}