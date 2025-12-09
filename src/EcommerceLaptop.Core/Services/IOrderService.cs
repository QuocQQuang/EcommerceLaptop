using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Order;
using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcommerceLaptop.Core.Services;

public interface IOrderService
{
    Task<OrderDto> CreateOrderFromCartAsync(int cartId, int customerId, string shippingAddress);
    Task<AtomicCheckoutResult> CreateOrderAndInitializePaymentAsync(CreateOrderRequest request);
    Task<OrderDetailsDto> GetOrderDetailsAsync(int orderId);
    Task<EcommerceLaptop.Core.DTOs.PagedResult<OrderDto>> GetCustomerOrdersAsync(int customerId, int page = 1, int pageSize = 10);
    Task<bool> UpdateOrderStatusAsync(int orderId, UpdateStatusRequest request);
    Task<bool> CancelOrderAsync(int orderId, CancelOrderRequest request);
    Task<EcommerceLaptop.Core.DTOs.PagedResult<OrderDto>> GetAdminOrdersAsync(int page = 1, int pageSize = 20, string? search = null, string? status = null, int? customerId = null);
    Task<EcommerceLaptop.Core.DTOs.PagedResult<AdminOrderDto>> GetEnhancedAdminOrdersAsync(int page = 1, int pageSize = 20, string? search = null, string? status = null, int? customerId = null);
    Task<EcommerceLaptop.Core.Entities.Payment?> GetPaymentByTransactionIdAsync(string transactionId);
}
