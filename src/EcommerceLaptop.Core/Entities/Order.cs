using EcommerceLaptop.Core.ValueObjects;

namespace EcommerceLaptop.Core.Entities;

public class Order
{
    private readonly List<OrderItem> _orderItems = new();
    private readonly List<OrderAudit> _audits = new();
    private readonly List<Payment> _payments = new();
    private readonly List<OrderEvent> _orderEvents = new();
    private readonly List<Refund> _refunds = new();

    // Factory Method
    public static Order Create(int userId, string orderNumber, EcommerceLaptop.Core.ValueObjects.Address shippingAddress)
    {
        var order = new Order
        {
            UserId = userId,
            OrderNumber = orderNumber,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            InventoryReserved = false,
            ShippingAddress = shippingAddress
        };
        
        return order;
    }

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public DateTime OrderDate { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal ShippingAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalAmount { get; private set; }

    // Shipping Information (Value Object)
    public EcommerceLaptop.Core.ValueObjects.Address ShippingAddress { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool InventoryReserved { get; private set; } = false;

    // Navigation properties
    // Navigation properties
    public User User { get; set; } = null!;
    
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();
    public IReadOnlyCollection<OrderAudit> Audits => _audits.AsReadOnly();
    public IReadOnlyCollection<OrderEvent> OrderEvents => _orderEvents.AsReadOnly();
    public IReadOnlyCollection<Refund> Refunds => _refunds.AsReadOnly();

    // Domain Methods

    public void AddItem(int productId, int quantity, decimal unitPrice, decimal itemDiscount = 0)
    {
        if (Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException("Cannot add items to an order that is not in Pending state.");
        }

        var existingItem = _orderItems.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.AddQuantity(quantity);
            existingItem.UpdatePrice(unitPrice, itemDiscount);
        }
        else
        {
            var item = new OrderItem(productId, quantity, unitPrice, itemDiscount);
            _orderItems.Add(item);
        }

        RecalculateTotals();
    }

    public void SetShippingAddress(EcommerceLaptop.Core.ValueObjects.Address address)
    {
        ShippingAddress = address ?? throw new ArgumentNullException(nameof(address));
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetFinancialDetails(decimal taxAmount, decimal shippingAmount, decimal discountAmount)
    {
        TaxAmount = taxAmount;
        ShippingAmount = shippingAmount;
        DiscountAmount = discountAmount;
        RecalculateTotals();
    }

    public void MarkAsInventoryReserved()
    {
        InventoryReserved = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReleaseInventoryReservation()
    {
        InventoryReserved = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Pay(string transactionId, PaymentGateway gateway, decimal amount)
    {
         if (Status == OrderStatus.Cancelled || Status == OrderStatus.Refunded)
        {
            throw new InvalidOperationException($"Cannot pay for an order in {Status} state.");
        }

        // Logic to add payment reference can be here or handled separately
        // Status transition
        Status = OrderStatus.Confirmed; // Or Processing
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsProcessing()
    {
        if (Status != OrderStatus.Confirmed && Status != OrderStatus.Pending)
        {
             // Flexible transition?
        }
        Status = OrderStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Ship()
    {
        if (Status != OrderStatus.Processing && Status != OrderStatus.Confirmed)
        {
            throw new InvalidOperationException("Order must be Processed or Confirmed before shipping.");
        }
        Status = OrderStatus.Shipped;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered)
        {
            throw new InvalidOperationException("Cannot cancel an order that has already been shipped or delivered.");
        }
        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending) return; // Idempotent or throw?
        
        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    private void RecalculateTotals()
    {
        SubTotal = _orderItems.Sum(x => x.TotalPrice);
        
        // Total = SubTotal + Tax + Shipping - Discount
        // Note: Logic might need to be adjusted if Tax/Discount are calculated differently (e.g. per item vs global)
        // Here we assume TaxAmount/DiscountAmount are provided externally or calculated elsewhere, 
        // OR we should calculate them here. 
        // For this refactor, we stick to summing SubTotal and using provided financial details.
        
        TotalAmount = SubTotal + TaxAmount + ShippingAmount - DiscountAmount;
        if (TotalAmount < 0) TotalAmount = 0;
    }

    public void Deliver()
    {
        if (Status != OrderStatus.Shipped)
        {
             // Flexible?
        }
        Status = OrderStatus.Delivered;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Return()
    {
         if (Status != OrderStatus.Delivered)
        {
            throw new InvalidOperationException("Only delivered orders can be returned.");
        }
        Status = OrderStatus.Returned;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Refund()
    {
        // Can be refunded from various states
        Status = OrderStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
    }  


}

public class OrderItem
{
    private OrderItem() { } // EF Core

    internal OrderItem(int productId, int quantity, decimal unitPrice, decimal discountAmount)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        DiscountAmount = discountAmount;
        CalculateTotalPrice();
    }

    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public int ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalPrice { get; private set; }

    // Navigation properties
    public Order Order { get; private set; } = null!;
    public Product Product { get; private set; } = null!;

    internal void AddQuantity(int quantity)
    {
        Quantity += quantity;
        CalculateTotalPrice();
    }

    internal void UpdatePrice(decimal unitPrice, decimal discountAmount)
    {
        UnitPrice = unitPrice;
        DiscountAmount = discountAmount;
        CalculateTotalPrice();
    }

    private void CalculateTotalPrice()
    {
        TotalPrice = (UnitPrice * Quantity) - DiscountAmount;
        if (TotalPrice < 0) TotalPrice = 0;
    }
}

public enum OrderStatus
{
    Pending = 1,
    Confirmed = 2,
    Processing = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6,
    Returned = 7,
    Refunded = 8
}

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public PaymentGateway Gateway { get; set; }
    public PaymentStatus Status { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string GatewayResponse { get; set; } = string.Empty; // JSON response from gateway

    // Navigation properties
    public Order Order { get; set; } = null!;
}

public enum PaymentGateway
{
    VnPay = 1,
    MoMo = 2,
    PayPal = 3,
    ZaloPay = 4,
    SePay = 5,
    Stripe = 6
}

public enum PaymentStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5,
    Refunded = 6
}

public enum PaymentMethod
{
    CreditCard = 1,
    DebitCard = 2,
    EWallet = 3,
    BankTransfer = 4,
    QRCode = 5,
    Installment = 6,
    CashOnDelivery = 7
}
