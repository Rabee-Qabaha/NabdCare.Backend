using NabdCare.Application.Common;

namespace NabdCare.Application.Interfaces.Permissions;

public interface IAccessPolicy<in TResource>
{
    /// <summary>
    /// Return true to allow, false to deny. Do not throw here.
    /// </summary>
    Task<bool> EvaluateAsync(ITenantContext user, string action, TResource resource);
}