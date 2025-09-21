namespace NabdCare.Application.Common;

public class TenantContext : ITenantContext
{
    public Guid? ClinicId { get; set; }
    public bool IsSuperAdmin { get; set; }
}