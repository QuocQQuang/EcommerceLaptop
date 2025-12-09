using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.API.DTOs;

#region Order DTOs

/// <summary>
/// Order data transfer object
/// </summary>
public class OrderDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Shipping Information
    public AddressDto ShippingAddress { get; set; } = new();
    public string ShippingProvider { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<OrderItemDto> OrderItems { get; set; } = new();
    public List<PaymentDto> Payments { get; set; } = new();
}

/// <summary>
/// Order item DTO
/// </summary>
public class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public string ProductBrand { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
}

/// <summary>
/// Address DTO
/// </summary>
public class AddressDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

/// <summary>
/// Payment DTO
/// </summary>
public class PaymentDto
{
    public int Id { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string Gateway { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string GatewayResponse { get; set; } = string.Empty;
}

#endregion

#region Order Request DTOs

/// <summary>
/// Create order request DTO
/// </summary>
public class CreateOrderRequest
{
    [Required]
    public List<CreateOrderItemRequest> Items { get; set; } = new();

    [Required]
    public CreateAddressRequest ShippingAddress { get; set; } = new();

    public string? CouponCode { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>
/// Create order item request DTO
/// </summary>
public class CreateOrderItemRequest
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

/// <summary>
/// Create address request DTO
/// </summary>
public class CreateAddressRequest
{
    [Required]
    [StringLength(255)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Province { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Country { get; set; } = string.Empty;
}

/// <summary>
/// Update order status request DTO
/// </summary>
public class UpdateOrderStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty; // Pending, Processing, Shipped, Delivered, Cancelled

    public string? TrackingNumber { get; set; }

    public string? ShippingProvider { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>
/// Process payment request DTO
/// </summary>
public class ProcessPaymentRequest
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = string.Empty; // CreditCard, DebitCard, PayPal, BankTransfer

    [Required]
    public string Gateway { get; set; } = string.Empty; // Stripe, PayPal, VNPay

    public Dictionary<string, object> PaymentDetails { get; set; } = new();
}

#endregion
