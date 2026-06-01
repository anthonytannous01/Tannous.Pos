using System.ComponentModel.DataAnnotations;
using Tannous.Pos.Domain.Common;
using Tannous.Pos.Domain.Common.ValueObjects;

namespace Tannous.Pos.Domain.Entities;

public class InventoryItem : BaseEntity, IAggregateRoot
{
    /// <summary>EF optimistic concurrency token (PostgreSQL bytea).</summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public decimal AverageCost { get; set; } = 0;
    public string Unit { get; set; } = "pcs";
    public DateTime LastUpdated { get; set; }
    
    // Foreign keys
    public Guid IngredientId { get; set; }
    
    // Navigation properties
    public virtual Ingredient Ingredient { get; set; } = null!;
    public virtual ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
}
