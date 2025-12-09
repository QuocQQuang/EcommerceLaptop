using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.Services.Payment;
using EcommerceLaptop.Core.DTOs.Payment;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Configuration;
using EcommerceLaptop.Core.Utilities.Payment;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace EcommerceLaptop.Infrastructure.Services.Payment;

/// <summary>
/// ZaloPay payment service implementation for Vietnamese mobile payments
/// Implements MAC signature validation and QR code payment support
/// </summary>
public class ZaloPayService : IZaloPayService
{
    private readonly ZaloPaySettings _settings;
    private readonly ILogger<ZaloPayService> _logger;
    private readonly HttpClient _httpClient;

    public PaymentGateway Gateway => PaymentGateway.ZaloPay;

    public ZaloPayService(IOptions<PaymentGatewaySettings> settings, ILogger<ZaloPayService> logger, HttpClient httpClient)
    {
        _settings = settings.Value.ZaloPay;
        _logger = logger;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Initializes ZaloPay payment with MAC signature
    /// </summary>
    public async Task<PaymentInitializationResult> InitializePaymentAsync(InitializePaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Initializing ZaloPay payment for order {OrderId}", request.OrderId);

            var result = new PaymentInitializationResult();

            // Validate request
            var validation = ValidateInitializationRequest(request);
            if (!validation.IsValid)
            {
                result.IsSuccess = false;
                result.ErrorMessage = string.Join(", ", validation.ValidationErrors);
                return result;
            }

            // Create ZaloPay order
            var transactionId = PaymentHelpers.GenerateTransactionId("ZALOPAY");
            var appTransId = $"{DateTime.Now:yyMMdd}_{transactionId}";
            var embedData = new
            {
                redirecturl = request.ReturnUrl ?? _settings.ReturnUrl,
                orderid = request.OrderId.ToString()
            };

            var orderData = new Dictionary<string, object>
            {
                ["app_id"] = _settings.AppId,
                ["app_user"] = "merchant", // Using fixed value since UserId is not in InitializePaymentRequest
                ["app_time"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["amount"] = (int)request.Amount, // ZaloPay uses VND in integer format
                ["app_trans_id"] = appTransId,
                ["embed_data"] = JsonSerializer.Serialize(embedData),
                ["item"] = JsonSerializer.Serialize(new[]
                {
                    new { name = request.Description ?? $"Payment for order {request.OrderId}", quantity = 1, price = (int)request.Amount }
                }),
                ["description"] = request.Description ?? $"Payment for order {request.OrderId}",
                ["bank_code"] = "zalopayapp"
            };

            // Generate MAC signature
            var signData = $"{orderData["app_id"]}|{orderData["app_trans_id"]}|{orderData["app_user"]}|{orderData["amount"]}|{orderData["app_time"]}|{orderData["embed_data"]}|{orderData["item"]}";
            var mac = GenerateZaloPayMac(signData, _settings.Key1);
            orderData["mac"] = mac;

            // Send request to ZaloPay
            var formContent = new FormUrlEncodedContent(orderData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString() ?? ""));
            var response = await _httpClient.PostAsync(_settings.CreateOrderUrl, formContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var zaloPayResponse = JsonSerializer.Deserialize<ZaloPayCreateOrderResponse>(responseContent);

                if (zaloPayResponse?.ReturnCode == 1)
                {
                    result.IsSuccess = true;
                    result.TransactionId = appTransId;
                    result.PaymentUrl = zaloPayResponse.OrderUrl;
                    result.ExpiresAt = DateTime.Now.AddMinutes(_settings.TimeoutInMinutes);
                    result.AdditionalData["zaloPayToken"] = zaloPayResponse.ZpTransToken;
                    result.AdditionalData["appTransId"] = appTransId;
                    result.AdditionalData["qrCodeUrl"] = await GenerateQrCodeAsync(appTransId);

                    _logger.LogInformation("ZaloPay payment initialized successfully. AppTransId: {AppTransId}", appTransId);
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = zaloPayResponse?.ReturnMessage ?? "Unknown error from ZaloPay";
                    result.ErrorCode = $"ZALOPAY_ERROR_{zaloPayResponse?.ReturnCode}";
                }
            }
            else
            {
                _logger.LogError("ZaloPay API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                result.IsSuccess = false;
                result.ErrorMessage = "Failed to communicate with ZaloPay";
                result.ErrorCode = "ZALOPAY_COMMUNICATION_ERROR";
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing ZaloPay payment for order {OrderId}", request.OrderId);
            return new PaymentInitializationResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to initialize payment",
                ErrorCode = "ZALOPAY_INIT_ERROR"
            };
        }
    }

    /// <summary>
    /// Processes ZaloPay payment response
    /// </summary>
    public async Task<PaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Processing ZaloPay payment response for order {OrderId}", request.OrderId);

            var result = new PaymentResult
            {
                TransactionId = request.TransactionId
            };

            // Query ZaloPay for transaction status
            var queryData = new Dictionary<string, string>
            {
                ["app_id"] = _settings.AppId.ToString(),
                ["app_trans_id"] = request.TransactionId
            };

            var querySignData = $"{queryData["app_id"]}|{queryData["app_trans_id"]}|{_settings.Key1}";
            var queryMac = GenerateZaloPayMac(querySignData, _settings.Key1);
            queryData["mac"] = queryMac;

            var queryContent = new FormUrlEncodedContent(queryData);
            var response = await _httpClient.PostAsync(_settings.QueryUrl, queryContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var queryResponse = JsonSerializer.Deserialize<ZaloPayQueryResponse>(responseContent);

                if (queryResponse?.ReturnCode == 1)
                {
                    result.IsSuccess = true;
                    result.Status = PaymentStatus.Completed;
                    result.Amount = queryResponse.Amount / 100m; // ZaloPay returns amount in VND cents
                    result.Currency = "VND";
                    result.ProcessedAt = DateTime.Now;
                    result.GatewayResponse = PaymentEncryptionUtils.SanitizeForLogging(responseContent);

                    result.Metadata["zaloPayTransId"] = queryResponse.ZpTransId.ToString();
                    result.Metadata["serverTime"] = queryResponse.ServerTime.ToString();
                    result.Metadata["discountAmount"] = queryResponse.DiscountAmount.ToString();

                    _logger.LogInformation("ZaloPay payment completed successfully. ZpTransId: {ZpTransId}", queryResponse.ZpTransId);
                }
                else if (queryResponse?.ReturnCode == 2)
                {
                    result.IsSuccess = false;
                    result.Status = PaymentStatus.Failed;
                    result.ErrorMessage = "Payment failed";
                    result.ErrorCode = "ZALOPAY_PAYMENT_FAILED";
                }
                else if (queryResponse?.ReturnCode == 3)
                {
                    result.IsSuccess = false;
                    result.Status = PaymentStatus.Pending;
                    result.ErrorMessage = "Payment is being processed";
                    result.ErrorCode = "ZALOPAY_PAYMENT_PROCESSING";
                }
                else
                {
                    result.IsSuccess = false;
                    result.Status = PaymentStatus.Failed;
                    result.ErrorMessage = queryResponse?.ReturnMessage ?? "Unknown error from ZaloPay";
                    result.ErrorCode = $"ZALOPAY_QUERY_ERROR_{queryResponse?.ReturnCode}";
                }
            }
            else
            {
                _logger.LogError("ZaloPay query error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                result.IsSuccess = false;
                result.Status = PaymentStatus.Failed;
                result.ErrorMessage = "Failed to query ZaloPay payment status";
                result.ErrorCode = "ZALOPAY_QUERY_ERROR";
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ZaloPay payment for order {OrderId}", request.OrderId);
            return new PaymentResult
            {
                IsSuccess = false,
                Status = PaymentStatus.Failed,
                TransactionId = request.TransactionId,
                ErrorMessage = "Failed to process payment response",
                ErrorCode = "ZALOPAY_PROCESS_ERROR"
            };
        }
    }

    /// <summary>
    /// Processes ZaloPay refund request
    /// </summary>
    public async Task<RefundResult> ProcessRefundAsync(ProcessRefundRequest request)
    {
        try
        {
            _logger.LogInformation("Processing ZaloPay refund for payment {PaymentId}", request.PaymentId);

            var refundId = PaymentHelpers.GenerateTransactionId("ZALOREFUND");
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var refundData = new Dictionary<string, object>
            {
                ["app_id"] = _settings.AppId,
                ["zp_trans_id"] = request.OriginalTransactionId,
                ["amount"] = (int)(request.RefundAmount * 100), // Convert to VND cents
                ["description"] = request.Reason ?? "Refund",
                ["timestamp"] = timestamp,
                ["m_refund_id"] = refundId
            };

            // Generate MAC for refund
            var refundSignData = $"{refundData["app_id"]}|{refundData["zp_trans_id"]}|{refundData["amount"]}|{refundData["description"]}|{refundData["timestamp"]}";
            var refundMac = GenerateZaloPayMac(refundSignData, _settings.Key1);
            refundData["mac"] = refundMac;

            var formContent = new FormUrlEncodedContent(refundData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString() ?? ""));
            var response = await _httpClient.PostAsync(_settings.RefundUrl, formContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            var result = new RefundResult
            {
                OriginalTransactionId = request.OriginalTransactionId,
                RefundAmount = request.RefundAmount,
                ProcessedAt = DateTime.Now
            };

            if (response.IsSuccessStatusCode)
            {
                var refundResponse = JsonSerializer.Deserialize<ZaloPayRefundResponse>(responseContent);

                result.IsSuccess = refundResponse?.ReturnCode == 1;
                result.RefundId = refundId;
                result.Status = refundResponse?.ReturnCode == 1 ? PaymentStatus.Refunded : PaymentStatus.Failed;
                result.GatewayResponse = PaymentEncryptionUtils.SanitizeForLogging(responseContent);

                if (!result.IsSuccess)
                {
                    result.ErrorMessage = refundResponse?.ReturnMessage ?? "Refund failed";
                    result.ErrorCode = $"ZALOPAY_REFUND_ERROR_{refundResponse?.ReturnCode}";
                }
            }
            else
            {
                result.IsSuccess = false;
                result.Status = PaymentStatus.Failed;
                result.ErrorMessage = "Failed to process ZaloPay refund";
                result.ErrorCode = "ZALOPAY_REFUND_ERROR";
            }

            _logger.LogInformation("ZaloPay refund processed. Success: {Success}, RefundId: {RefundId}",
                result.IsSuccess, result.RefundId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ZaloPay refund for payment {PaymentId}", request.PaymentId);
            return new RefundResult
            {
                IsSuccess = false,
                OriginalTransactionId = request.OriginalTransactionId,
                RefundAmount = request.RefundAmount,
                Status = PaymentStatus.Failed,
                ErrorMessage = "Failed to process refund",
                ErrorCode = "ZALOPAY_REFUND_ERROR"
            };
        }
    }

    /// <summary>
    /// Gets payment status from ZaloPay
    /// </summary>
    public async Task<PaymentStatus> GetPaymentStatusAsync(string transactionId)
    {
        try
        {
            var queryData = new Dictionary<string, string>
            {
                ["app_id"] = _settings.AppId.ToString(),
                ["app_trans_id"] = transactionId
            };

            var querySignData = $"{queryData["app_id"]}|{queryData["app_trans_id"]}|{_settings.Key1}";
            var queryMac = GenerateZaloPayMac(querySignData, _settings.Key1);
            queryData["mac"] = queryMac;

            var queryContent = new FormUrlEncodedContent(queryData);
            var response = await _httpClient.PostAsync(_settings.QueryUrl, queryContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var queryResponse = JsonSerializer.Deserialize<ZaloPayQueryResponse>(responseContent);
                return MapZaloPayStatus(queryResponse?.ReturnCode ?? -1);
            }

            return PaymentStatus.Failed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ZaloPay payment status for transaction {TransactionId}", transactionId);
            return PaymentStatus.Failed;
        }
    }

    /// <summary>
    /// Validates ZaloPay webhook signature
    /// </summary>
    public Task<bool> ValidateWebhookAsync(string payload, string signature, IDictionary<string, string> headers)
    {
        try
        {
            // Parse ZaloPay callback data
            var callbackData = JsonSerializer.Deserialize<ZaloPayCallback>(payload);
            if (callbackData == null) return Task.FromResult(false);

            // Verify MAC signature
            var callbackSignData = $"{callbackData.AppId}|{callbackData.AppTransId}|{callbackData.AppUser}|{callbackData.Amount}|{callbackData.AppTime}|{callbackData.EmbedData}|{callbackData.Item}";
            var computedMac = GenerateZaloPayMac(callbackSignData, _settings.Key2);

            var isValid = string.Equals(computedMac, callbackData.Mac, StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                _logger.LogWarning("ZaloPay webhook signature validation failed. Expected: {Expected}, Received: {Received}", computedMac, callbackData.Mac);
            }

            return Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating ZaloPay webhook signature");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Processes ZaloPay webhook notification
    /// </summary>
    public Task<PaymentWebhookResult> ProcessWebhookAsync(string payload, IDictionary<string, string> headers)
    {
        try
        {
            var callbackData = JsonSerializer.Deserialize<ZaloPayCallback>(payload);

            if (callbackData == null)
            {
                return Task.FromResult(new PaymentWebhookResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid webhook data format",
                    RequiresResponse = false
                });
            }

            // ZaloPay webhook indicates successful payment
            var status = callbackData.Type == 1 ? PaymentStatus.Completed : PaymentStatus.Failed;

            var result = new PaymentWebhookResult
            {
                IsSuccess = true,
                TransactionId = callbackData.AppTransId,
                Status = status,
                Action = status == PaymentStatus.Completed ? "payment_completed" : "payment_failed",
                Data = new Dictionary<string, object>
                {
                    ["zpTransId"] = callbackData.ZpTransId,
                    ["serverTime"] = callbackData.ServerTime,
                    ["channel"] = callbackData.Channel,
                    ["merchantUserId"] = callbackData.MerchantUserId,
                    ["amount"] = callbackData.Amount,
                    ["discountAmount"] = callbackData.DiscountAmount
                },
                RequiresResponse = true
            };

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ZaloPay webhook");
            return Task.FromResult(new PaymentWebhookResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to process webhook",
                RequiresResponse = false
            });
        }
    }

    /// <summary>
    /// Checks if payment method is supported by ZaloPay
    /// </summary>
    public bool SupportsPaymentMethod(PaymentMethod method)
    {
        return PaymentHelpers.IsPaymentMethodSupported(method, PaymentGateway.ZaloPay);
    }

    /// <summary>
    /// Gets supported currencies for ZaloPay (VND only)
    /// </summary>
    public IReadOnlyList<string> GetSupportedCurrencies()
    {
        return new[] { "VND" }.AsReadOnly();
    }

    /// <summary>
    /// Generates QR code for ZaloPay payment
    /// </summary>
    public async Task<QrCodeResult> GenerateQrCodeAsync(string transactionId)
    {
        try
        {
            var qrData = new Dictionary<string, string>
            {
                ["app_id"] = _settings.AppId.ToString(),
                ["app_trans_id"] = transactionId
            };

            var qrSignData = $"{qrData["app_id"]}|{qrData["app_trans_id"]}|{_settings.Key1}";
            var qrMac = GenerateZaloPayMac(qrSignData, _settings.Key1);
            qrData["mac"] = qrMac;

            var qrContent = new FormUrlEncodedContent(qrData);
            var response = await _httpClient.PostAsync(_settings.GetQrUrl, qrContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var qrResponse = JsonSerializer.Deserialize<ZaloPayQrResponse>(responseContent);

                if (qrResponse?.ReturnCode == 1)
                {
                    return new QrCodeResult
                    {
                        IsSuccess = true,
                        QrCodeUrl = qrResponse.QrCodeUrl,
                        QrCodeData = qrResponse.QrCodeData,
                        ExpiresAt = DateTime.Now.AddMinutes(_settings.TimeoutInMinutes)
                    };
                }
            }

            return new QrCodeResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to generate QR code"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating ZaloPay QR code for transaction {TransactionId}", transactionId);
            return new QrCodeResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to generate QR code"
            };
        }
    }

    /// <summary>
    /// Gets ZaloPay app deep link for mobile payments
    /// </summary>
    public Task<MobileDeepLinkResult> GenerateDeepLinkAsync(string transactionId)
    {
        try
        {
            // ZaloPay uses its own app scheme
            var deepLink = $"zalopay://payment/{transactionId}";

            var result = new MobileDeepLinkResult
            {
                IsSuccess = true,
                DeepLink = deepLink,
                AppStoreUrl = "https://apps.apple.com/vn/app/zalopay/id1112649927",
                PlayStoreUrl = "https://play.google.com/store/apps/details?id=vn.com.vng.zalopay",
                ExpiresAt = DateTime.Now.AddMinutes(_settings.TimeoutInMinutes)
            };

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating ZaloPay deep link for transaction {TransactionId}", transactionId);
            return Task.FromResult(new MobileDeepLinkResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to generate deep link"
            });
        }
    }

    /// <summary>
    /// Gets ZaloPay transaction limits
    /// </summary>
    public TransactionLimitsResult GetTransactionLimits()
    {
        return new TransactionLimitsResult
        {
            MinAmount = 10000, // 10,000 VND
            MaxAmount = 20000000, // 20,000,000 VND (20M VND)
            Currency = "VND",
            DailyLimit = 50000000, // 50M VND per day
            MonthlyLimit = 100000000 // 100M VND per month
        };
    }

    #region Private Helper Methods

    private PaymentValidationResult ValidateInitializationRequest(InitializePaymentRequest request)
    {
        var result = PaymentHelpers.ValidateAmount(request.Amount, PaymentGateway.ZaloPay, request.Currency);

        if (request.Currency != "VND")
        {
            result.IsValid = false;
            result.ValidationErrors.Add("ZaloPay only supports VND currency");
        }

        if (request.Amount < 10000 || request.Amount > 20000000)
        {
            result.IsValid = false;
            result.ValidationErrors.Add("Amount must be between 10,000 and 20,000,000 VND");
        }

        return result;
    }

    private string GenerateZaloPayMac(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }

    private PaymentStatus MapZaloPayStatus(int returnCode)
    {
        return returnCode switch
        {
            1 => PaymentStatus.Completed,
            2 => PaymentStatus.Failed,
            3 => PaymentStatus.Pending,
            _ => PaymentStatus.Failed
        };
    }

    #endregion

    #region Data Transfer Objects

    private class ZaloPayCreateOrderResponse
    {
        public int ReturnCode { get; set; }
        public string ReturnMessage { get; set; } = string.Empty;
        public string OrderUrl { get; set; } = string.Empty;
        public string ZpTransToken { get; set; } = string.Empty;
        public string AppTransId { get; set; } = string.Empty;
    }

    private class ZaloPayQueryResponse
    {
        public int ReturnCode { get; set; }
        public string ReturnMessage { get; set; } = string.Empty;
        public bool IsProcessing { get; set; }
        public long Amount { get; set; }
        public long DiscountAmount { get; set; }
        public long ZpTransId { get; set; }
        public long ServerTime { get; set; }
    }

    private class ZaloPayRefundResponse
    {
        public int ReturnCode { get; set; }
        public string ReturnMessage { get; set; } = string.Empty;
        public string RefundId { get; set; } = string.Empty;
    }

    private class ZaloPayQrResponse
    {
        public int ReturnCode { get; set; }
        public string ReturnMessage { get; set; } = string.Empty;
        public string QrCodeUrl { get; set; } = string.Empty;
        public string QrCodeData { get; set; } = string.Empty;
    }

    private class ZaloPayCallback
    {
        public int AppId { get; set; }
        public string AppTransId { get; set; } = string.Empty;
        public string AppUser { get; set; } = string.Empty;
        public long AppTime { get; set; }
        public string EmbedData { get; set; } = string.Empty;
        public string Item { get; set; } = string.Empty;
        public long ZpTransId { get; set; }
        public long ServerTime { get; set; }
        public int Channel { get; set; }
        public string MerchantUserId { get; set; } = string.Empty;
        public long Amount { get; set; }
        public long DiscountAmount { get; set; }
        public int Type { get; set; }
        public string Mac { get; set; } = string.Empty;
    }

    #endregion
}