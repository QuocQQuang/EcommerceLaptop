using System;
using System.Collections.Generic;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.DTOs.Payment;

namespace EcommerceLaptop.Core.DTOs.Order;

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public string? PaymentStatus { get; set; } // Add payment status
    public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
}

public class OrderDetailsDto : OrderDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public List<OrderAuditDto> AuditTrail { get; set; } = new List<OrderAuditDto>();
}

public class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class OrderAuditDto
{
    public OrderStatus OldStatus { get; set; }
    public OrderStatus NewStatus { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}

public class UpdateStatusRequest
{
    public OrderStatus NewStatus { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class CancelOrderRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class CreateOrderRequest
{
    public int CartId { get; set; }
    public int CustomerId { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingProvince { get; set; } = string.Empty;
    public string ShippingPostalCode { get; set; } = string.Empty;
    public string ShippingCountry { get; set; } = string.Empty;

    // Payment information for atomic checkout
    public PaymentGateway PaymentGateway { get; set; }
    public string? PaymentMethod { get; set; }
    public string ReturnUrl { get; set; } = string.Empty;
    public string? CancelUrl { get; set; }
}

public class AtomicCheckoutResult
{
    public OrderDto Order { get; set; } = null!;
    public PaymentInitializationResult Payment { get; set; } = null!;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
