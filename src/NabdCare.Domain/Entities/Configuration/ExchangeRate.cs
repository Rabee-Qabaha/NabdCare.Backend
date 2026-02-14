using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NabdCare.Domain.Entities.Configuration;

public class ExchangeRate : BaseEntity
{
    [Required]
    [MaxLength(3)]
    public string BaseCurrency { get; set; }

    [Required]
    [MaxLength(3)]
    public string TargetCurrency { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 6)")]
    public decimal Rate { get; set; }

    [Required]
    public DateTime LastUpdated { get; set; }
}