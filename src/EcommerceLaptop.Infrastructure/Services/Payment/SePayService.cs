using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.Services.Payment;
using EcommerceLaptop.Core.DTOs.Payment;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Configuration;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace EcommerceLaptop.Infrastructure.Services.Payment;

/// <summary>
/// SePay transaction monitoring service implementation
/// SePay is NOT a payment gateway - it's a transaction monitoring service
/// Monitors bank account transactions and provides webhook notifications
/// </summary>
public class SePayService : ISePayService
{
    private readonly SePaySettings _settings;
    private readonly ILogger<SePayService> _logger;
    private readonly HttpClient _httpClient;

    public PaymentGateway Gateway => PaymentGateway.SePay;

    // Supported banks based on SePay documentation
    private static readonly IReadOnlyList<(string BankCode, string BankName)> SupportedBanks = new List<(string, string)>
    {
        ("VPB", "VPBank"),
        ("BIDV", "BIDV"),
        ("TPB", "TPBank"),
        ("ACB", "ACB"),
        ("CTG", "VietinBank"),
        ("MB", "MB Bank"),
        ("OCB", "OCB"),
        ("KLB", "KienLongBank"),
        ("MSB", "MSB")
    }.AsReadOnly();

    public SePayService(IOptions<PaymentGatewaySettings> settings, ILogger<SePayService> logger, HttpClient httpClient)
    {
        _settings = settings.Value.SePay;
        _logger = logger;
        _httpClient = httpClient;

        // Configure HttpClient for SePay API according to official documentation
        // Authentication uses "Apikey API_KEY" format (not "Bearer")
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Apikey {_settings.ApiToken}");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Generates QR code for bank transfer using official SePay QR service
    /// URL format: https://qr.sepay.vn/img?acc=ACCOUNT&bank=BANK&amount=AMOUNT&des=DESCRIPTION
    /// According to official documentation at https://qr.sepay.vn/
    /// </summary>
    public async Task<SePayQrCodeResponse> GenerateQrCodeAsync(SePayQrCodeRequest request)
    {
        try
        {
            await Task.Delay(1); // Placeholder for async operations
            
            _logger.LogInformation("Generating SePay QR code for order {OrderId}, amount {Amount}", 
                request.OrderId, request.Amount);

            // Get default bank account if not specified
            var bankAccount = string.IsNullOrEmpty(request.BankAccount) 
                ? GetDefaultBankAccount() 
                : GetBankAccountByNumber(request.BankAccount);

            if (bankAccount == null)
            {
                _logger.LogError("No valid bank account found for QR generation");
                return new SePayQrCodeResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "Bank account not configured or found"
                };
            }

            // Format description for order tracking (Vietnamese format)
            var description = FormatOrderDescription(request.OrderId, null);
            
            // Build official SePay QR code URL according to documentation
            // https://qr.sepay.vn/img?acc=SO_TAI_KHOAN&bank=NGAN_HANG&amount=SO_TIEN&des=NOI_DUNG
            var qrCodeUrl = $"https://qr.sepay.vn/img?" +
                           $"acc={HttpUtility.UrlEncode(bankAccount.AccountNumber)}&" +
                           $"bank={HttpUtility.UrlEncode(bankAccount.BankCode)}&" +
                           $"amount={((long)request.Amount)}&" +
                           $"des={HttpUtility.UrlEncode(description)}";

            // Generate VietQR data (standard format)
            var vietQrData = GenerateVietQrData(bankAccount, request.Amount, description);

            var response = new SePayQrCodeResponse
            {
                IsSuccess = true,
                QrCodeUrl = qrCodeUrl,
                QrCodeData = vietQrData,
                BankAccount = bankAccount.AccountNumber,
                BankName = bankAccount.BankName,
                Amount = request.Amount,
                Description = description,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15) // QR codes expire in 15 minutes
            };

            _logger.LogInformation("SePay QR code generated successfully for order {OrderId}: {QrUrl}", request.OrderId, qrCodeUrl);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SePay QR code for order {OrderId}", request.OrderId);
            return new SePayQrCodeResponse
            {
                IsSuccess = false,
                ErrorMessage = $"QR code generation failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Processes webhook notification from SePay
    /// Validates transaction data and maps to existing orders
    /// </summary>
    public async Task<SePayWebhookResult> ProcessWebhookAsync(SePayWebhookPayload payload, IDictionary<string, string> headers)
    {
        try
        {
            _logger.LogInformation("Processing SePay webhook for transaction {TransactionId}, amount {Amount}", 
                payload.Id, payload.TransferAmount);

            // Check for duplicate transactions (per SePay official documentation)
            if (await IsDuplicateTransactionAsync(payload))
            {
                _logger.LogWarning("Duplicate SePay transaction detected: {TransactionId}", payload.Id);
                return new SePayWebhookResult
                {
                    Success = true, // Return success to acknowledge duplicate but don't process
                    Message = "Duplicate transaction ignored",
                    ResponseContent = JsonSerializer.Serialize(new { success = true })
                };
            }

            // Validate webhook payload
            if (!ValidateWebhookPayload(payload))
            {
                _logger.LogWarning("Invalid SePay webhook payload received");
                return new SePayWebhookResult
                {
                    Success = false,
                    Message = "Invalid webhook payload"
                };
            }

            // Only process incoming transfers
            if (payload.TransferType != "in")
            {
                _logger.LogDebug("Ignoring outgoing transaction {TransactionId}", payload.Id);
                return new SePayWebhookResult
                {
                    Success = true,
                    Message = "Outgoing transaction ignored"
                };
            }

            // Extract order ID from transaction content
            var orderId = ExtractOrderIdFromContent(payload.Content);
            if (!orderId.HasValue)
            {
                _logger.LogWarning("No order ID found in transaction content: {Content}", payload.Content);
                return new SePayWebhookResult
                {
                    Success = true,
                    Message = "No order reference found"
                };
            }

            // Extract payment code if present
            var paymentCode = ExtractPaymentCode(payload.Content);

            _logger.LogInformation("SePay transaction {TransactionId} mapped to order {OrderId}", 
                payload.Id, orderId.Value);

            return new SePayWebhookResult
            {
                Success = true,
                Message = "Transaction processed successfully",
                OrderId = orderId.Value,
                PaymentCode = paymentCode,
                ProcessedAt = DateTime.UtcNow,
                ResponseContent = JsonSerializer.Serialize(new { success = true })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SePay webhook for transaction {TransactionId}", payload.Id);
            return new SePayWebhookResult
            {
                Success = false,
                Message = $"Webhook processing failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Validates webhook authentication using official SePay format
    /// According to documentation: "Authorization":"Apikey API_KEY_CUA_BAN"
    /// </summary>
    public async Task<bool> ValidateWebhookAuthAsync(string authHeader)
    {
        try
        {
            await Task.Delay(1); // Placeholder for async auth operations
            
            if (string.IsNullOrEmpty(authHeader))
            {
                _logger.LogWarning("SePay webhook received without authorization header");
                return false;
            }

            // Check official SePay Apikey format (not Bearer)
            if (!authHeader.StartsWith("Apikey ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("SePay webhook authorization header has invalid format. Expected 'Apikey API_KEY', got: {AuthHeader}", 
                    authHeader.Substring(0, Math.Min(20, authHeader.Length)) + "...");
                return false;
            }

            var token = authHeader.Substring(7).Trim();
            var isValid = token == _settings.ApiToken;

            if (!isValid)
            {
                _logger.LogWarning("SePay webhook received with invalid API key");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating SePay webhook authentication");
            return false;
        }
    }

    /// <summary>
    /// Retrieves transaction history from SePay API
    /// Rate limit: 2 requests per second
    /// </summary>
    public async Task<SePayMonitoringResponse> GetTransactionsAsync(SePayMonitoringRequest request)
    {
        try
        {
            _logger.LogInformation("Retrieving SePay transactions for account {Account}, page {Page}", 
                request.BankAccount ?? "all", request.Page);

            // Build API request URL - this would be the actual SePay API endpoint
            // Note: In real implementation, this would call SePay's monitoring API
            var queryParams = new List<string>();
            
            if (request.FromDate.HasValue)
                queryParams.Add($"from_date={request.FromDate.Value:yyyy-MM-dd}");
            
            if (request.ToDate.HasValue)
                queryParams.Add($"to_date={request.ToDate.Value:yyyy-MM-dd}");
            
            if (!string.IsNullOrEmpty(request.BankAccount))
                queryParams.Add($"account={request.BankAccount}");
            
            if (!string.IsNullOrEmpty(request.TransferType))
                queryParams.Add($"type={request.TransferType}");
            
            queryParams.Add($"page={request.Page}");
            queryParams.Add($"limit={request.PageSize}");

            var apiUrl = $"{_settings.ApiBaseUrl}/transactions?" + string.Join("&", queryParams);

            // Apply rate limiting (2 requests per second)
            await ApplyRateLimitAsync();

            var response = await _httpClient.GetAsync(apiUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("SePay API request failed with status {StatusCode}", response.StatusCode);
                return new SePayMonitoringResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"API request failed: {response.StatusCode}"
                };
            }

            var content = await response.Content.ReadAsStringAsync();
            var apiResult = JsonSerializer.Deserialize<SePayApiResponse>(content);

            return new SePayMonitoringResponse
            {
                IsSuccess = true,
                Transactions = apiResult?.Data?.Select(MapToTransactionInfo).ToList() ?? new List<SePayTransactionInfo>(),
                TotalCount = apiResult?.TotalCount ?? 0,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SePay transactions");
            return new SePayMonitoringResponse
            {
                IsSuccess = false,
                ErrorMessage = $"Transaction retrieval failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Registers a new bank account for monitoring (placeholder implementation)
    /// </summary>
    public async Task<bool> RegisterBankAccountAsync(SePayBankAccountRequest request)
    {
        try
        {
            await Task.Delay(1); // Placeholder for async API operations
            
            _logger.LogInformation("Registering bank account {AccountNumber} for SePay monitoring", 
                request.AccountNumber);

            // In real implementation, this would call SePay's account registration API
            // For now, we'll simulate success if the bank is supported
            var supportedBank = SupportedBanks.Any(b => b.BankCode.Equals(request.BankCode, StringComparison.OrdinalIgnoreCase));
            
            if (!supportedBank)
            {
                _logger.LogWarning("Unsupported bank code: {BankCode}", request.BankCode);
                return false;
            }

            _logger.LogInformation("Bank account {AccountNumber} registered successfully", request.AccountNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering SePay bank account {AccountNumber}", request.AccountNumber);
            return false;
        }
    }

    /// <summary>
    /// Gets list of supported banks
    /// </summary>
    public IReadOnlyList<(string BankCode, string BankName)> GetSupportedBanks()
    {
        return SupportedBanks;
    }

    /// <summary>
    /// Checks SePay service health
    /// </summary>
    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            // Simple health check by trying to reach the QR service
            var healthUrl = _settings.QrBaseUrl.Replace("/img", "/health");
            var response = await _httpClient.GetAsync(healthUrl);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SePay health check failed");
            return false;
        }
    }

    /// <summary>
    /// Extracts order ID from transaction description following SePay best practices
    /// Looks for patterns like "DH123", "Order 456", "Don hang 789"
    /// Enhanced with more Vietnamese patterns and better accuracy
    /// </summary>
    public int? ExtractOrderIdFromContent(string content)
    {
        if (string.IsNullOrEmpty(content))
            return null;

        _logger.LogDebug("Extracting order ID from content: {Content}", content);

        // Enhanced patterns for order references in Vietnamese - ordered by priority
        var patterns = new[]
        {
            @"DH0*(\d+)",                         // DH123, DH000123 (highest priority)
            @"Don\s*hang\s*(?:so\s*)?0*(\d+)",   // Don hang 123, Don hang so 123
            @"Order\s*(?:number\s*)?0*(\d+)",     // Order 123, Order number 123
            @"HD0*(\d+)",                         // HD123 (hoa don)
            @"Bill\s*(?:no\s*)?0*(\d+)",         // Bill 123, Bill no 123
            @"Dat\s*hang\s*0*(\d+)",             // Dat hang 123
            @"Ma\s*don\s*hang\s*0*(\d+)",        // Ma don hang 123
            @"#0*(\d+)",                          // #123
            @"OD0*(\d+)",                         // OD123
            @"INV0*(\d+)",                        // INV123 (invoice)
            @"SO0*(\d+)"                          // SO123 (sales order)
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var orderId))
            {
                _logger.LogDebug("Order ID {OrderId} extracted using pattern: {Pattern}", orderId, pattern);
                return orderId;
            }
        }

        _logger.LogDebug("No order ID pattern matched for content: {Content}", content);
        return null;
    }

    /// <summary>
    /// Formats standardized order description for bank transfer
    /// </summary>
    public string FormatOrderDescription(int orderId, string? customerName = null)
    {
        var description = $"DH{orderId:D6}"; // DH000123 format
        
        if (!string.IsNullOrEmpty(customerName))
        {
            // Limit customer name to avoid exceeding bank transfer description limits
            var cleanName = Regex.Replace(customerName, @"[^\w\s]", "").Trim();
            if (cleanName.Length > 20)
                cleanName = cleanName.Substring(0, 20);
            
            description += $" {cleanName}";
        }

        return description;
    }

    #region Private Helper Methods

    /// <summary>
    /// Rate limiting for SePay API (2 requests per second as per official documentation)
    /// </summary>
    private static readonly SemaphoreSlim _rateLimitSemaphore = new(1, 1);
    private static DateTime _lastApiCall = DateTime.MinValue;

    private async Task ApplyRateLimitAsync()
    {
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            var timeSinceLastCall = DateTime.UtcNow - _lastApiCall;
            var minimumInterval = TimeSpan.FromMilliseconds(500); // 2 requests per second = 500ms interval

            if (timeSinceLastCall < minimumInterval)
            {
                var delayTime = minimumInterval - timeSinceLastCall;
                await Task.Delay(delayTime);
            }

            _lastApiCall = DateTime.UtcNow;
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    private SePayBankAccount? GetDefaultBankAccount()
    {
        return _settings.MonitoredAccounts?.FirstOrDefault(a => a.IsDefault);
    }

    private SePayBankAccount? GetBankAccountByNumber(string accountNumber)
    {
        return _settings.MonitoredAccounts?.FirstOrDefault(a => a.AccountNumber == accountNumber);
    }

    private string BuildQrCodeUrl(Dictionary<string, string> parameters)
    {
        var queryString = string.Join("&", parameters.Select(kv => $"{kv.Key}={kv.Value}"));
        return $"{_settings.QrBaseUrl}?{queryString}";
    }

    private string GenerateVietQrData(SePayBankAccount account, decimal amount, string description)
    {
        // Simplified VietQR format - in real implementation this would follow the full VietQR standard
        return $"VietQR|{account.BankCode}|{account.AccountNumber}|{amount:F0}|{description}";
    }

    /// <summary>
    /// Validates webhook payload according to official SePay documentation
    /// </summary>
    private bool ValidateWebhookPayload(SePayWebhookPayload payload)
    {
        return payload.Id > 0 &&
               !string.IsNullOrEmpty(payload.Gateway) &&
               !string.IsNullOrEmpty(payload.AccountNumber) &&
               !string.IsNullOrEmpty(payload.TransferType) &&
               payload.TransferAmount > 0 &&
               !string.IsNullOrEmpty(payload.TransactionDate) &&
               !string.IsNullOrEmpty(payload.Content);
    }

    /// <summary>
    /// Checks for duplicate transactions following SePay official documentation
    /// Uses transaction ID as primary check, with backup validation using
    /// referenceCode + transferType + transferAmount combination
    /// </summary>
    private async Task<bool> IsDuplicateTransactionAsync(SePayWebhookPayload payload)
    {
        try
        {
            await Task.Delay(1); // Placeholder for async database operations
            
            // Primary check: Transaction ID uniqueness (recommended by SePay docs)
            // In real implementation, this would query database for existing transaction
            // For now, we'll use in-memory cache as demonstration
            
            var transactionKey = $"sepay_txn_{payload.Id}";
            
            // TODO: Replace with actual database query
            // Example: var existingTransaction = await _dbContext.SePayTransactions
            //                                      .FirstOrDefaultAsync(t => t.SepayTransactionId == payload.Id);
            
            _logger.LogDebug("Checking duplicate transaction for SePay ID: {TransactionId}", payload.Id);
            
            // Secondary check: Combination of referenceCode + transferType + transferAmount
            // This provides additional protection against edge cases
            if (!string.IsNullOrEmpty(payload.ReferenceCode))
            {
                var combinationKey = $"sepay_combo_{payload.ReferenceCode}_{payload.TransferType}_{payload.TransferAmount}";
                
                _logger.LogDebug("Checking duplicate transaction combination: {CombinationKey}", combinationKey);
                
                // TODO: Replace with actual database query for combination check
                // Example: var existingCombo = await _dbContext.SePayTransactions
                //                               .FirstOrDefaultAsync(t => t.ReferenceCode == payload.ReferenceCode &&
                //                                                        t.TransferType == payload.TransferType &&
                //                                                        t.TransferAmount == payload.TransferAmount);
            }
            
            // For demonstration, always return false (no duplicates)
            // In real implementation, return true if transaction already exists
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking duplicate transaction for SePay ID: {TransactionId}", payload.Id);
            // On error, assume not duplicate to avoid blocking legitimate transactions
            return false;
        }
    }

    private string? ExtractPaymentCode(string content)
    {
        // Extract payment/reference codes from transaction content
        var codePattern = @"(?:Ma GD|Ref|Code)[\s:]*([A-Z0-9]+)";
        var match = Regex.Match(content, codePattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private SePayTransactionInfo MapToTransactionInfo(SePayApiTransaction apiTransaction)
    {
        return new SePayTransactionInfo
        {
            Id = apiTransaction.Id,
            Gateway = apiTransaction.Gateway,
            TransactionDate = apiTransaction.TransactionDate,
            AccountNumber = apiTransaction.AccountNumber,
            Content = apiTransaction.Content,
            Amount = apiTransaction.TransferAmount,
            TransferType = apiTransaction.TransferType,
            ReferenceCode = apiTransaction.ReferenceCode,
            IsProcessed = false,
            OrderId = ExtractOrderIdFromContent(apiTransaction.Content)
        };
    }

    #endregion

    #region Helper Classes for API Response

    private class SePayApiResponse
    {
        public List<SePayApiTransaction> Data { get; set; } = new();
        public int TotalCount { get; set; }
    }

    private class SePayApiTransaction
    {
        public int Id { get; set; }
        public string Gateway { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public decimal TransferAmount { get; set; }
        public string TransferType { get; set; } = string.Empty;
        public string ReferenceCode { get; set; } = string.Empty;
    }

    #endregion
}