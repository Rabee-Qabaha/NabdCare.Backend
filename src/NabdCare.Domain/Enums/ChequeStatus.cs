using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Enums;

[ExportTsEnum]
    public enum ChequeStatus
    {
        Pending, 
        Cleared, 
        Bounced, 
        Cancelled
    }
