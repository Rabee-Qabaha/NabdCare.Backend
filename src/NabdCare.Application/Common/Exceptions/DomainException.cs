namespace NabdCare.Application.Common.Exceptions;

public class DomainException : Exception
{
    public string ErrorCode { get; }
    
    /// <summary>
    /// Optional: The name of the specific field causing the error (e.g., "Email", "Name").
    /// If set, the frontend will highlight this input instead of showing a generic toast.
    /// </summary>
    public string? TargetField { get; }

    public DomainException(string message, string errorCode = "DOMAIN_ERROR", string? targetField = null)
        : base(message)
    {
        ErrorCode = errorCode;
        TargetField = targetField;
    }
}