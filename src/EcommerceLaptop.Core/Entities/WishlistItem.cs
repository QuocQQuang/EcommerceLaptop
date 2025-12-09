using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Core.Entities
{
    public class WishlistItem
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}