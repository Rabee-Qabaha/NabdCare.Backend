using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NabdCare.Domain.Entities.Clinics;

namespace NabdCare.Domain.Entities.Inventory;

public class Product : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? Sku { get; set; }

    public bool IsService { get; set; } // True = Consultation, False = Toothbrush
    public bool TrackStock { get; set; } = true;

    [Column(TypeName = "decimal(18,2)")]
    public decimal CostPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SellPrice { get; set; }

    public int CurrentStock { get; set; }
    public int LowStockThreshold { get; set; } = 5;
}