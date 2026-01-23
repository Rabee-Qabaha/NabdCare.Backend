using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Enums;

[ExportTsEnum]
public enum DocumentType
{
    Other = 0,
    
    // Identity
    NationalId = 1,
    InsuranceCard = 2,
    
    // Clinical
    LabResult = 10,
    XRay = 11,
    MRI = 12,
    ReferralLetter = 13,
    PrescriptionScan = 14,
    
    // Legal
    ConsentForm = 20
}