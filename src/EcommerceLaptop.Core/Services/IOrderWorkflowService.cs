using EcommerceLaptop.Core.Entities;
using System.Threading.Tasks;

namespace EcommerceLaptop.Core.Services;

public interface IOrderWorkflowService
{
    Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string reason, int? changedByUserId = null);
    Task<bool> CancelOrderAsync(int orderId, string reason);
    bool CanTransitionTo(OrderStatus currentStatus, OrderStatus newStatus);
}
