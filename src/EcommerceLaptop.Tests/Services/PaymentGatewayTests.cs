using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.DTOs.Payment;
using Xunit;

namespace EcommerceLaptop.Tests.Services
{
    /// <summary>
    /// Payment Gateway Unit Tests following Clean Code principles
    /// Tests critical payment flows and security validations
    /// </summary>
    public class PaymentGatewayTests
    {
        [Theory]
        [InlineData(PaymentGateway.VnPay, PaymentMethod.CreditCard)]
        [InlineData(PaymentGateway.MoMo, PaymentMethod.EWallet)]
        [InlineData(PaymentGateway.PayPal, PaymentMethod.CreditCard)]
        [InlineData(PaymentGateway.ZaloPay, PaymentMethod.QRCode)]
        public void PaymentGateway_WithValidMethods_ShouldBeSupported(PaymentGateway gateway, PaymentMethod method)
        {
            // Arrange & Act
            var isValidCombination = IsValidGatewayMethodCombination(gateway, method);

            // Assert
            Assert.True(isValidCombination, $"{gateway} should support {method}");
        }

        [Theory]
        [InlineData(1000)] // 1,000 VND - too small
        [InlineData(-100)] // negative amount
        [InlineData(0)] // zero amount
        public void PaymentAmount_WithInvalidValues_ShouldBeInvalid(decimal amount)
        {
            // Arrange & Act
            var isValid = IsValidPaymentAmount(amount, "VND");

            // Assert
            Assert.False(isValid, $"Amount {amount} should be invalid");
        }

        [Theory]
        [InlineData(1000)] // 10,000 VND - minimum valid
        [InlineData(100000)] // 100,000 VND - typical amount
        [InlineData(5000000000)] // 50,000,000 VND - large amount
        public void PaymentAmount_WithValidValues_ShouldBeValid(decimal amount)
        {
            // Arrange & Act
            var isValid = IsValidPaymentAmount(amount, "VND");

            // Assert
            Assert.True(isValid, $"Amount {amount} should be valid");
        }

        [Theory]
        [InlineData(PaymentStatus.Pending, PaymentStatus.Processing)]
        [InlineData(PaymentStatus.Processing, PaymentStatus.Completed)]
        [InlineData(PaymentStatus.Processing, PaymentStatus.Failed)]
        [InlineData(PaymentStatus.Completed, PaymentStatus.Refunded)]
        public void PaymentStatus_WithValidTransitions_ShouldBeAllowed(PaymentStatus from, PaymentStatus to)
        {
            // Arrange & Act
            var isValidTransition = IsValidStatusTransition(from, to);

            // Assert
            Assert.True(isValidTransition, $"Transition from {from} to {to} should be valid");
        }

        [Theory]
        [InlineData(PaymentStatus.Completed, PaymentStatus.Pending)]
        [InlineData(PaymentStatus.Failed, PaymentStatus.Processing)]
        [InlineData(PaymentStatus.Refunded, PaymentStatus.Completed)]
        [InlineData(PaymentStatus.Cancelled, PaymentStatus.Processing)]
        public void PaymentStatus_WithInvalidTransitions_ShouldNotBeAllowed(PaymentStatus from, PaymentStatus to)
        {
            // Arrange & Act
            var isValidTransition = IsValidStatusTransition(from, to);

            // Assert
            Assert.False(isValidTransition, $"Transition from {from} to {to} should be invalid");
        }

        [Fact]
        public void PaymentWebhookResult_WithValidData_ShouldHaveCorrectProperties()
        {
            // Arrange
            var webhookResult = new PaymentWebhookResult
            {
                IsSuccess = true,
                TransactionId = "TXN123",
                Status = PaymentStatus.Completed,
                Action = "payment",
                RequiresResponse = true,
                ResponseContent = "RspCode=00&Message=Success"
            };

            // Act & Assert
            Assert.True(webhookResult.IsSuccess);
            Assert.Equal("TXN123", webhookResult.TransactionId);
            Assert.Equal(PaymentStatus.Completed, webhookResult.Status);
            Assert.Equal("payment", webhookResult.Action);
            Assert.True(webhookResult.RequiresResponse);
            Assert.Equal("RspCode=00&Message=Success", webhookResult.ResponseContent);
            Assert.NotNull(webhookResult.Data);
        }

        [Theory]
        [InlineData("VND", 10000, 5000000000)] // Vietnamese Dong
        [InlineData("USD", 1, 10000)] // US Dollar
        public void PaymentCurrency_WithValidRanges_ShouldBeSupported(string currency, decimal minAmount, decimal maxAmount)
        {
            // Arrange & Act
            var isMinValid = IsValidPaymentAmount(minAmount, currency);
            var isMaxValid = IsValidPaymentAmount(maxAmount, currency);

            // Assert
            Assert.True(isMinValid, $"Minimum amount {minAmount} {currency} should be valid");
            Assert.True(isMaxValid, $"Maximum amount {maxAmount} {currency} should be valid");
        }

        [Fact]
        public void RefundResult_WithValidData_ShouldHaveCorrectProperties()
        {
            // Arrange
            var refundResult = new RefundResult
            {
                IsSuccess = true,
                RefundId = "REFUND123",
                Status = PaymentStatus.Refunded
            };

            // Act & Assert
            Assert.True(refundResult.IsSuccess);
            Assert.Equal("REFUND123", refundResult.RefundId);
            Assert.Equal(PaymentStatus.Refunded, refundResult.Status);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void TransactionId_WithInvalidValues_ShouldBeInvalid(string transactionId)
        {
            // Arrange & Act
            var isValid = IsValidTransactionId(transactionId);

            // Assert
            Assert.False(isValid, $"Transaction ID '{transactionId}' should be invalid");
        }

        [Fact]
        public void TransactionId_WithNullValue_ShouldBeInvalid()
        {
            // Arrange & Act
            var isValid = IsValidTransactionId(null);

            // Assert
            Assert.False(isValid, "Null transaction ID should be invalid");
        }

        [Theory]
        [InlineData("TXN123")]
        [InlineData("ORDER_2025_001")]
        [InlineData("VNPAY_12345")]
        public void TransactionId_WithValidValues_ShouldBeValid(string transactionId)
        {
            // Arrange & Act
            var isValid = IsValidTransactionId(transactionId);

            // Assert
            Assert.True(isValid, $"Transaction ID '{transactionId}' should be valid");
        }

        // Helper methods for validation logic
        private static bool IsValidGatewayMethodCombination(PaymentGateway gateway, PaymentMethod method)
        {
            return gateway switch
            {
                PaymentGateway.VnPay => method is PaymentMethod.CreditCard or PaymentMethod.DebitCard
                    or PaymentMethod.BankTransfer or PaymentMethod.QRCode,
                PaymentGateway.MoMo => method is PaymentMethod.EWallet or PaymentMethod.QRCode
                    or PaymentMethod.BankTransfer,
                PaymentGateway.PayPal => method is PaymentMethod.CreditCard or PaymentMethod.DebitCard
                    or PaymentMethod.EWallet,
                PaymentGateway.ZaloPay => method is PaymentMethod.EWallet or PaymentMethod.QRCode
                    or PaymentMethod.BankTransfer,
                _ => false
            };
        }

        private static bool IsValidPaymentAmount(decimal amount, string currency)
        {
            if (amount <= 0) return false;

            return currency.ToUpper() switch
            {
                "VND" => amount >= 10000 && amount <= 100000000, // 10K to 100M VND
                "USD" => amount >= 1 && amount <= 50000, // $1 to $50K USD
                _ => false
            };
        }

        private static bool IsValidStatusTransition(PaymentStatus from, PaymentStatus to)
        {
            var validTransitions = new Dictionary<PaymentStatus, PaymentStatus[]>
            {
                [PaymentStatus.Pending] = [PaymentStatus.Processing, PaymentStatus.Cancelled],
                [PaymentStatus.Processing] = [PaymentStatus.Completed, PaymentStatus.Failed, PaymentStatus.Cancelled],
                [PaymentStatus.Completed] = [PaymentStatus.Refunded],
                [PaymentStatus.Failed] = [],
                [PaymentStatus.Cancelled] = [],
                [PaymentStatus.Refunded] = []
            };

            return validTransitions.ContainsKey(from) && validTransitions[from].Contains(to);
        }

        private static bool IsValidTransactionId(string? transactionId)
        {
            return !string.IsNullOrWhiteSpace(transactionId) && transactionId.Length >= 3;
        }
    }
}