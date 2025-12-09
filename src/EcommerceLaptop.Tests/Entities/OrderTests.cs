using Xunit;
using FluentAssertions;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Tests.Entities;

public class OrderTests
{
    [Fact]
    public void Order_Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var order = new Order
        {
            UserId = 1,
            OrderNumber = "ORD-2025-001",
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            SubTotal = 1000.00m,
            TaxAmount = 100.00m,
            ShippingAmount = 50.00m,
            DiscountAmount = 0.00m,
            TotalAmount = 1150.00m,
            ShippingStreet = "123 Test Street",
            ShippingCity = "Ho Chi Minh City",
            InventoryReserved = true
        };

        // Assert
        order.UserId.Should().Be(1);
        order.OrderNumber.Should().Be("ORD-2025-001");
        order.Status.Should().Be(OrderStatus.Pending);
        order.SubTotal.Should().Be(1000.00m);
        order.TotalAmount.Should().Be(1150.00m);
        order.ShippingCity.Should().Be("Ho Chi Minh City");
        order.InventoryReserved.Should().BeTrue();
        order.CreatedAt.Should().NotBe(default(DateTime));
        order.UpdatedAt.Should().NotBe(default(DateTime));
    }

    [Fact]
    public void Order_NavigationProperties_ShouldInitializeEmptyCollections()
    {
        // Arrange & Act
        var order = new Order();

        // Assert
        order.OrderItems.Should().NotBeNull().And.BeEmpty();
        order.Payments.Should().NotBeNull().And.BeEmpty();
        order.Audits.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void OrderItem_Properties_ShouldBeSettable()
    {
        // Arrange & Act
        var orderItem = new OrderItem
        {
            OrderId = 1,
            ProductId = 10,
            Quantity = 2,
            UnitPrice = 500.00m,
            DiscountAmount = 50.00m,
            TotalPrice = 950.00m
        };

        // Assert
        orderItem.OrderId.Should().Be(1);
        orderItem.ProductId.Should().Be(10);
        orderItem.Quantity.Should().Be(2);
        orderItem.UnitPrice.Should().Be(500.00m);
        orderItem.TotalPrice.Should().Be(950.00m);
    }

    [Fact]
    public void Payment_Properties_ShouldBeSettable()
    {
        // Arrange & Act
        var payment = new Payment
        {
            OrderId = 1,
            TransactionId = "TXN-12345",
            Gateway = PaymentGateway.VnPay,
            Status = PaymentStatus.Completed,
            Method = PaymentMethod.CreditCard,
            Amount = 1150.00m,
            Currency = "VND",
            GatewayResponse = "{\"status\":\"success\"}"
        };

        // Assert
        payment.OrderId.Should().Be(1);
        payment.TransactionId.Should().Be("TXN-12345");
        payment.Gateway.Should().Be(PaymentGateway.VnPay);
        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.Method.Should().Be(PaymentMethod.CreditCard);
        payment.Amount.Should().Be(1150.00m);
        payment.GatewayResponse.Should().Be("{\"status\":\"success\"}");
        payment.CreatedAt.Should().NotBe(default(DateTime));
    }

    [Theory]
    [InlineData(OrderStatus.Pending, 1)]
    [InlineData(OrderStatus.Shipped, 4)]
    [InlineData(OrderStatus.Delivered, 5)]
    public void OrderStatus_Enum_ShouldHaveCorrectValues(OrderStatus status, int expectedValue)
    {
        // Act & Assert
        ((int)status).Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(PaymentGateway.VnPay, 1)]
    [InlineData(PaymentGateway.MoMo, 2)]
    [InlineData(PaymentGateway.PayPal, 3)]
    public void PaymentGateway_Enum_ShouldHaveCorrectValues(PaymentGateway gateway, int expectedValue)
    {
        // Act & Assert
        ((int)gateway).Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending, 1)]
    [InlineData(PaymentStatus.Completed, 3)]
    [InlineData(PaymentStatus.Failed, 4)]
    public void PaymentStatus_Enum_ShouldHaveCorrectValues(PaymentStatus status, int expectedValue)
    {
        // Act & Assert
        ((int)status).Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(PaymentMethod.CreditCard, 1)]
    [InlineData(PaymentMethod.EWallet, 3)]
    [InlineData(PaymentMethod.QRCode, 5)]
    public void PaymentMethod_Enum_ShouldHaveCorrectValues(PaymentMethod method, int expectedValue)
    {
        // Act & Assert
        ((int)method).Should().Be(expectedValue);
    }
}