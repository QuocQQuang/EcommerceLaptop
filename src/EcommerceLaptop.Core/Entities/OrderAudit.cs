using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceLaptop.Core.Entities;

public class OrderAudit
{
    public int Id { get; set; }

    [ForeignKey(nameof(Order))]
    public int OrderId { get; set; }

    public OrderStatus OldStatus { get; set; }
    public OrderStatus NewStatus { get; set; }

    public int? ChangedByUserId { get; set; } // Null for system changes
    public string Reason { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Order Order { get; set; } = null!;
    public virtual User? ChangedByUser { get; set; }
}
