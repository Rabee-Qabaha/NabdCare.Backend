using NabdCare.Domain.Enums;
using TypeGen.Core.TypeAnnotations;

namespace NabdCare.Application.DTOs.Payments;

[ExportTsClass]
public class UpdateChequeStatusDto
{
    public ChequeStatus Status { get; set; }
}