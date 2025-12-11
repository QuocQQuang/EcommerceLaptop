using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.Order;

public class OrderItemsByUserSpecification : BaseSpecification<OrderItem>
{
    public OrderItemsByUserSpecification(int userId) 
        : base(oi => oi.Order != null && oi.Order.UserId == userId)
    {
        AddInclude(oi => oi.Product);
        AddInclude(oi => oi.Order);
    }
}
