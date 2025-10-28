using Microsoft.AspNetCore.Authorization;

namespace NabdCare.Api.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
    {
        Policy = $"PERMISSION:{permission}";
    }
}