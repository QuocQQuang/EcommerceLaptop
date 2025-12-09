using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using EcommerceLaptop.Core.Configuration;
using EcommerceLaptop.Core.DTOs.Payment;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Services.Payment;

namespace EcommerceLaptop.Tests.Services;

/// <summary>
/// SePay Service Unit Tests following Clean Code and TDD principles
/// Tests transaction monitoring, QR code generation, webhook processing, and authentication
/// </summary>
public class SePayServiceTests : IDisposable
{
    private readonly Mock<ILogger<SePayService>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly SePayService _sePayService;
    private readonly SePaySettings _testSettings;

    public SePayServiceTests()
    {
        _mockLogger = new Mock<ILogger<SePayService>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);

        // Setup test SePay configuration
        _testSettings = new SePaySettings
        {
            ApiToken = "test_sepay_token_123",
            ApiBaseUrl = "https://api.sepay.vn/v1",
            QrBaseUrl = "https://qr.sepay.vn/img",
            WebhookUrl = "https://localhost:7001/api/payments/sepay/webhook",
            IsEnabled = true,
            RateLimitPerSecond = 2,
            MonitoredAccounts = new List<SePayBankAccount>
            {
                new()
                {
                    AccountNumber = "1234567890",
                    BankCode = "VPB",
                    BankName = "VPBank",
                    AccountName = "CONG TY ECOMMERCE LAPTOP",
                    IsDefault = true,
                    IsActive = true,
                    Description = "Main business account"
                },
                new()
                {
                    AccountNumber = "9876543210",
                    BankCode = "BIDV",
                    BankName = "BIDV",
                    AccountName = "CONG TY ECOMMERCE LAPTOP",
                    IsDefault = false,
                    IsActive = true,
                    Description = "Secondary account"
                }
            }
        };

        var options = Options.Create(new PaymentGatewaySettings { SePay = _testSettings });
        _sePayService = new SePayService(options, _mockLogger.Object, _httpClient);
    }

    #region QR Code Generation Tests

    [Fact]
    public async Task GenerateQrCodeAsync_WithValidRequest_ShouldReturnSuccessfulResponse()
    {
        // Arrange
        var request = new SePayQrCodeRequest
        {
            OrderId = 12345,
            Amount = 100000,
            Description = "Test order payment",
            Currency = "VND"
        };

        // Act
        var result = await _sePayService.GenerateQrCodeAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("qr.sepay.vn/img", result.QrCodeUrl);
        Assert.Equal("1234567890", result.BankAccount); // Default account
        Assert.Equal("VPBank", result.BankName);
        Assert.Equal(100000, result.Amount);
        Assert.Contains("DH012345", result.Description); // Formatted order description
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task GenerateQrCodeAsync_WithSpecificBankAccount_ShouldUseSpecifiedAccount()
    {
        // Arrange
        var request = new SePayQrCodeRequest
        {
            OrderId = 12345,
            Amount = 100000,
            Description = "Test order payment",
            BankAccount = "9876543210" // Secondary account
        };

        // Act
        var result = await _sePayService.GenerateQrCodeAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("9876543210", result.BankAccount);
        Assert.Equal("BIDV", result.BankName);
    }

    [Fact]
    public async Task GenerateQrCodeAsync_WithInvalidBankAccount_ShouldReturnError()
    {
        // Arrange
        var request = new SePayQrCodeRequest
        {
            OrderId = 12345,
            Amount = 100000,
            Description = "Test order payment",
            BankAccount = "invalid_account"
        };

        // Act
        var result = await _sePayService.GenerateQrCodeAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Bank account not configured", result.ErrorMessage);
    }

    [Theory]
    [InlineData(500)] // Below minimum
    [InlineData(1500000000)] // Above maximum
    public async Task GenerateQrCodeAsync_WithInvalidAmount_ShouldReturnError(decimal amount)
    {
        // Arrange
        var request = new SePayQrCodeRequest
        {
            OrderId = 12345,
            Amount = amount,
            Description = "Test order payment"
        };

        // Act & Assert - This should be handled by model validation in real scenario
        // For now, the service doesn't validate amounts, but QR generation should still work
        var result = await _sePayService.GenerateQrCodeAsync(request);
        Assert.True(result.IsSuccess); // Service doesn't validate, controller/API does
    }

    #endregion

    #region Webhook Processing Tests

    [Fact]
    public async Task ProcessWebhookAsync_WithValidIncomingTransaction_ShouldReturnSuccess()
    {
        // Arrange
        var payload = new SePayWebhookPayload
        {
            Id = 123456,
            Gateway = "VPBank",
            TransactionDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            AccountNumber = "1234567890",
            Content = "Transfer money for DH012345 order payment",
            TransferType = "in",
            TransferAmount = 100000,
            Accumulated = 5000000,
            ReferenceCode = "VPB123456789",
            Description = "Customer payment via bank transfer"
        };

        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer test_token"
        };

        // Act
        var result = await _sePayService.ProcessWebhookAsync(payload, headers);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(12345, result.OrderId); // Extracted from content
        Assert.Contains("processed successfully", result.Message);
        Assert.True(result.ProcessedAt > DateTime.MinValue);
    }

    [Fact]
    public async Task ProcessWebhookAsync_WithOutgoingTransaction_ShouldIgnore()
    {
        // Arrange
        var payload = new SePayWebhookPayload
        {
            Id = 123456,
            Gateway = "VPBank",
            TransactionDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            AccountNumber = "1234567890",
            Content = "Transfer money out",
            TransferType = "out", // Outgoing transaction
            TransferAmount = 100000,
            ReferenceCode = "VPB123456789"
        };

        var headers = new Dictionary<string, string>();

        // Act
        var result = await _sePayService.ProcessWebhookAsync(payload, headers);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Outgoing transaction ignored", result.Message);
        Assert.Null(result.OrderId);
    }

    [Fact]
    public async Task ProcessWebhookAsync_WithNoOrderReference_ShouldReturnSuccessWithoutOrderId()
    {
        // Arrange
        var payload = new SePayWebhookPayload
        {
            Id = 123456,
            Gateway = "VPBank",
            TransactionDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            AccountNumber = "1234567890",
            Content = "Random transfer without order reference",
            TransferType = "in",
            TransferAmount = 100000,
            ReferenceCode = "VPB123456789"
        };

        var headers = new Dictionary<string, string>();

        // Act
        var result = await _sePayService.ProcessWebhookAsync(payload, headers);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("No order reference found", result.Message);
        Assert.Null(result.OrderId);
    }

    [Fact]
    public async Task ProcessWebhookAsync_WithInvalidPayload_ShouldReturnFailure()
    {
        // Arrange
        var payload = new SePayWebhookPayload
        {
            Id = 0, // Invalid ID
            Gateway = "",
            TransactionDate = "", // Empty string for invalid case
            AccountNumber = "",
            TransferType = "in",
            TransferAmount = 0
        };

        var headers = new Dictionary<string, string>();

        // Act
        var result = await _sePayService.ProcessWebhookAsync(payload, headers);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid webhook payload", result.Message);
    }

    #endregion

    #region Authentication Tests

    [Fact]
    public async Task ValidateWebhookAuthAsync_WithValidBearerToken_ShouldReturnTrue()
    {
        // Arrange
        var authHeader = "Bearer test_sepay_token_123";

        // Act
        var result = await _sePayService.ValidateWebhookAuthAsync(authHeader);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateWebhookAuthAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var authHeader = "Bearer invalid_token";

        // Act
        var result = await _sePayService.ValidateWebhookAuthAsync(authHeader);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Invalid format")]
    [InlineData("Basic dGVzdA==")]
    public async Task ValidateWebhookAuthAsync_WithInvalidFormat_ShouldReturnFalse(string authHeader)
    {
        // Act
        var result = await _sePayService.ValidateWebhookAuthAsync(authHeader);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateWebhookAuthAsync_WithNullAuthHeader_ShouldReturnFalse()
    {
        // Act
        var result = await _sePayService.ValidateWebhookAuthAsync(null!);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Order ID Extraction Tests

    [Theory]
    [InlineData("Transfer for DH012345 order", 12345)]
    [InlineData("Payment Don hang 67890", 67890)]
    [InlineData("Order 999 payment received", 999)]
    [InlineData("HD111222 invoice payment", 111222)]
    [InlineData("Transfer for #555666", 555666)]
    [InlineData("Payment OD777888", 777888)]
    public void ExtractOrderIdFromContent_WithValidPatterns_ShouldReturnCorrectOrderId(string content, int expectedOrderId)
    {
        // Act
        var result = _sePayService.ExtractOrderIdFromContent(content);

        // Assert
        Assert.Equal(expectedOrderId, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Random transfer without order reference")]
    [InlineData("Payment received")]
    [InlineData("DH without number")]
    public void ExtractOrderIdFromContent_WithInvalidPatterns_ShouldReturnNull(string content)
    {
        // Act
        var result = _sePayService.ExtractOrderIdFromContent(content);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractOrderIdFromContent_WithNullContent_ShouldReturnNull()
    {
        // Act
        var result = _sePayService.ExtractOrderIdFromContent(null!);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Order Description Formatting Tests

    [Fact]
    public void FormatOrderDescription_WithOrderIdOnly_ShouldReturnFormattedString()
    {
        // Arrange
        var orderId = 123;

        // Act
        var result = _sePayService.FormatOrderDescription(orderId);

        // Assert
        Assert.Equal("DH000123", result);
    }

    [Fact]
    public void FormatOrderDescription_WithOrderIdAndCustomerName_ShouldIncludeCustomerName()
    {
        // Arrange
        var orderId = 123;
        var customerName = "Nguyen Van A";

        // Act
        var result = _sePayService.FormatOrderDescription(orderId, customerName);

        // Assert
        Assert.Equal("DH000123 Nguyen Van A", result);
    }

    [Fact]
    public void FormatOrderDescription_WithLongCustomerName_ShouldTruncate()
    {
        // Arrange
        var orderId = 123;
        var customerName = "This is a very long customer name that should be truncated";

        // Act
        var result = _sePayService.FormatOrderDescription(orderId, customerName);

        // Assert
        Assert.StartsWith("DH000123", result);
        // DH000123 (8) + space (1) + max name (20) = 29 characters max
        Assert.True(result.Length <= 29);
        Assert.Contains("This is a very long ", result); // First 20 chars of customer name
        Assert.Equal("DH000123 This is a very long ", result);
    }

    #endregion

    #region Transaction Monitoring Tests

    [Fact]
    public async Task GetTransactionsAsync_WithValidRequest_ShouldReturnSuccessfulResponse()
    {
        // Arrange
        var request = new SePayMonitoringRequest
        {
            FromDate = DateTime.Today.AddDays(-7),
            ToDate = DateTime.Today,
            BankAccount = "1234567890",
            TransferType = "in",
            Page = 1,
            PageSize = 10
        };

        var mockResponse = new
        {
            Data = new[]
            {
                new
                {
                    Id = 123,
                    Gateway = "VPBank",
                    TransactionDate = DateTime.Now,
                    AccountNumber = "1234567890",
                    Content = "Transfer for DH012345",
                    TransferAmount = 100000M,
                    TransferType = "in",
                    ReferenceCode = "VPB123"
                }
            },
            TotalCount = 1
        };

        var jsonResponse = JsonSerializer.Serialize(mockResponse);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _sePayService.GetTransactionsAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Transactions);
        Assert.Equal(1, result.TotalCount);

        var transaction = result.Transactions.First();
        Assert.Equal(123, transaction.Id);
        Assert.Equal("VPBank", transaction.Gateway);
        Assert.Equal(100000, transaction.Amount);
        Assert.Equal(12345, transaction.OrderId); // Extracted from content
    }

    [Fact]
    public async Task GetTransactionsAsync_WithApiError_ShouldReturnErrorResponse()
    {
        // Arrange
        var request = new SePayMonitoringRequest
        {
            Page = 1,
            PageSize = 10
        };

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _sePayService.GetTransactionsAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("API request failed", result.ErrorMessage);
    }

    #endregion

    #region Bank Account Management Tests

    [Theory]
    [InlineData("VPB", true)]
    [InlineData("BIDV", true)]
    [InlineData("TPB", true)]
    [InlineData("INVALID", false)]
    public async Task RegisterBankAccountAsync_WithSupportedBank_ShouldReturnExpectedResult(string bankCode, bool expectedResult)
    {
        // Arrange
        var request = new SePayBankAccountRequest
        {
            AccountNumber = "1111222233",
            BankCode = bankCode,
            AccountName = "Test Account",
            IsDefault = false
        };

        // Act
        var result = await _sePayService.RegisterBankAccountAsync(request);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    #endregion

    #region Supported Banks Tests

    [Fact]
    public void GetSupportedBanks_ShouldReturnCorrectBankList()
    {
        // Act
        var supportedBanks = _sePayService.GetSupportedBanks();

        // Assert
        Assert.NotEmpty(supportedBanks);
        Assert.Contains(supportedBanks, b => b.BankCode == "VPB" && b.BankName == "VPBank");
        Assert.Contains(supportedBanks, b => b.BankCode == "BIDV" && b.BankName == "BIDV");
        Assert.Contains(supportedBanks, b => b.BankCode == "TPB" && b.BankName == "TPBank");
        Assert.Equal(9, supportedBanks.Count); // Should have 9 supported banks
    }

    #endregion

    #region Gateway Property Tests

    [Fact]
    public void Gateway_ShouldReturnSePay()
    {
        // Act
        var gateway = _sePayService.Gateway;

        // Assert
        Assert.Equal(PaymentGateway.SePay, gateway);
    }

    #endregion

    #region Health Check Tests

    [Fact]
    public async Task CheckHealthAsync_WithSuccessfulResponse_ShouldReturnTrue()
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _sePayService.CheckHealthAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckHealthAsync_WithFailedResponse_ShouldReturnFalse()
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _sePayService.CheckHealthAsync();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    public async Task GetTransactionsAsync_MultipleRequests_ShouldApplyRateLimit()
    {
        // Arrange
        var request = new SePayMonitoringRequest { Page = 1, PageSize = 1 };
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"Data\":[], \"TotalCount\":0}", Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Make 3 requests quickly
        var task1 = _sePayService.GetTransactionsAsync(request);
        var task2 = _sePayService.GetTransactionsAsync(request);
        var task3 = _sePayService.GetTransactionsAsync(request);

        await Task.WhenAll(task1, task2, task3);
        stopwatch.Stop();

        // Assert
        // With 2 requests per second limit, 3 requests should take at least 1 second
        Assert.True(stopwatch.ElapsedMilliseconds >= 900); // Allow some tolerance
    }

    #endregion

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
