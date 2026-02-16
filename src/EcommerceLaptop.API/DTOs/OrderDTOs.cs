using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.API.DTOs;

#region Order DTOs

/// <summary>
/// Order data transfer object
/// </summary>
public record OrderDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string UserEmail { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public DateTime OrderDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal SubTotal { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal ShippingAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalAmount { get; init; }

    // Shipping Information
    public AddressDto ShippingAddress { get; init; } = new();
    public string ShippingProvider { get; init; } = string.Empty;
    public string TrackingNumber { get; init; } = string.Empty;
    public DateTime? ShippedDate { get; init; }
    public DateTime? DeliveredDate { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public List<OrderItemDto> OrderItems { get; init; } = new();
    public List<PaymentDto> Payments { get; init; } = new();
}

/// <summary>
/// Order item DTO
/// </summary>
public record OrderItemDto
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ProductSKU { get; init; } = string.Empty;
    public string ProductBrand { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalPrice { get; init; }
}

/// <summary>
/// Address DTO
/// </summary>
public record AddressDto
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Province { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
}

/// <summary>
/// Payment DTO
/// </summary>
public record PaymentDto
{
    public int Id { get; init; }
    public string TransactionId { get; init; } = string.Empty;
    public string Gateway { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public string GatewayResponse { get; init; } = string.Empty;
}

#endregion

#region Order Request DTOs

/// <summary>
/// Create order request DTO
/// </summary>
public record CreateOrderRequest
{
    [Required]
    public List<CreateOrderItemRequest> Items { get; init; } = new();

    [Required]
    public CreateAddressRequest ShippingAddress { get; init; } = new();

    public string? CouponCode { get; init; }

    [StringLength(1000)]
    public string? Notes { get; init; }
}

/// <summary>
/// Create order item request DTO
/// </summary>
public record CreateOrderItemRequest
{
    [Required]
    public int ProductId { get; init; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }
}

/// <summary>
/// Create address request DTO
/// </summary>
public record CreateAddressRequest
{
    [Required]
    [StringLength(255)]
    public string Street { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Province { get; init; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PostalCode { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Country { get; init; } = string.Empty;
}

/// <summary>
/// Update order status request DTO
/// </summary>
public record UpdateOrderStatusRequest
{
    [Required]
    public string Status { get; init; } = string.Empty; // Pending, Processing, Shipped, Delivered, Cancelled

    public string? TrackingNumber { get; init; }

    public string? ShippingProvider { get; init; }

    [StringLength(1000)]
    public string? Notes { get; init; }
}

/// <summary>
/// Process payment request DTO
/// </summary>
public record ProcessPaymentRequest
{
    [Required]
    public int OrderId { get; init; }

    [Required]
    public string PaymentMethod { get; init; } = string.Empty; // CreditCard, DebitCard, PayPal, BankTransfer

    [Required]
    public string Gateway { get; init; } = string.Empty; // Stripe, PayPal, SePay

    public Dictionary<string, object> PaymentDetails { get; init; } = new();
}

#endregion
