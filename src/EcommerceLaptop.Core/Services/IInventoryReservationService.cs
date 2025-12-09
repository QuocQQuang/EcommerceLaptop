using EcommerceLaptop.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcommerceLaptop.Core.Services;

public interface IInventoryReservationService
{
    Task<bool> ReserveInventoryForOrderAsync(int orderId);
    Task<bool> ReserveInventoryInternalAsync(int orderId);
    Task<bool> ReleaseInventoryForOrderAsync(int orderId);
    Task<InventoryValidationResult> ValidateCartItemsAvailabilityAsync(IEnumerable<CartItem> cartItems);
    Task<bool> UpdateReservedQuantityAsync(int productId, int quantityChange);
}

public class InventoryValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
}
