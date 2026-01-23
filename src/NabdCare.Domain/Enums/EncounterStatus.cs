using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Domain.Enums;

[ExportTsEnum]
public enum EncounterStatus
{
    /// <summary>
    /// The doctor is currently working on the notes. 
    /// Data is mutable. Not visible to billing yet.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// The doctor has signed the notes. 
    /// Record is now IMMUTABLE (cannot be changed). 
    /// Triggers the Billing/Invoice process.
    /// </summary>
    Finalized = 1,

    /// <summary>
    /// If a finalized record needs correction, it is marked as Amended.
    /// The original remains in history; a new version is created.
    /// </summary>
    Amended = 2,

    /// <summary>
    /// The encounter was created in error and should be ignored.
    /// </summary>
    Canceled = 3
}