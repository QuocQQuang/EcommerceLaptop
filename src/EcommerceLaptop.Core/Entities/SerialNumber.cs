using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceLaptop.Core.Entities;

public class SerialNumber
{
    public int Id { get; set; }
    
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Value { get; set; } = string.Empty; // The actual SN
    
    public SerialNumberStatus Status { get; set; } = SerialNumberStatus.Available;
    
    [StringLength(50)]
    public string BatchNumber { get; set; } = string.Empty;
    
    public DateTime DateReceived { get; set; }
    public DateTime? DateSold { get; set; }
    
    public string? OrderReference { get; set; } // Link to order (optional)

    // Navigation properties
    public Product Product { get; set; } = null!;
}

public enum SerialNumberStatus
{
    Available = 0,
    Reserved = 1,
    Sold = 2,
    Defective = 3,
    Returned = 4
}
