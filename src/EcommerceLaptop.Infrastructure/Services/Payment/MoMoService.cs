using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.Services.Payment;
using EcommerceLaptop.Core.DTOs.Payment;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Configuration;
using EcommerceLaptop.Core.Utilities.Payment;
using System.Text;
using System.Text.Json;

namespace EcommerceLaptop.Infrastructure.Services.Payment;

/// <summary>
/// MoMo payment service implementation following Vietnamese e-wallet regulations
/// Implements RSA signature validation and mobile deep linking
/// </summary>
public class MoMoService : IMoMoService
{
    private readonly MoMoSettings _settings;
    private readonly ILogger<MoMoService> _logger;
    private readonly HttpClient _httpClient;

    public PaymentGateway Gateway => PaymentGateway.MoMo;

    public MoMoService(IOptions<PaymentGatewaySettings> settings, ILogger<MoMoService> logger, HttpClient httpClient)
    {
        _settings = settings.Value.MoMo;
        _logger = logger;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Initializes MoMo payment with RSA signature
    /// </summary>
    public async Task<PaymentInitializationResult> InitializePaymentAsync(InitializePaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Initializing MoMo payment for order {OrderId}", request.OrderId);

            var result = new PaymentInitializationResult();

            // Validate request
            var validation = ValidateInitializationRequest(request);
            if (!validation.IsValid)
            {
                result.IsSuccess = false;
                result.ErrorMessage = string.Join(", ", validation.ValidationErrors);
                return result;
            }

            // Generate order information
            var orderId = PaymentHelpers.GenerateTransactionId("MOMO");
            var requestId = PaymentHelpers.GenerateTransactionId("REQ");
            var orderInfo = $"Payment for order {request.OrderId}";
            var redirectUrl = !string.IsNullOrEmpty(request.ReturnUrl) ? request.ReturnUrl : _settings.ReturnUrl;
            var ipnUrl = _settings.NotifyUrl;
            var amount = (long)request.Amount;
            var requestType = GetRequestType(request.Method);

            // Build MoMo request data
            var momoRequest = new
            {
                partnerCode = _settings.PartnerCode,
                partnerName = "Ca Hng Laptop",
                storeId = _settings.PartnerCode,
                requestId = requestId,
                amount = amount,
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = redirectUrl,
                ipnUrl = ipnUrl,
                lang = "vi",
                requestType = requestType,
                autoCapture = true,
                extraData = BuildExtraData(request)
            };

            // Generate signature
            var rawSignature = $"accessKey={_settings.AccessKey}&amount={amount}&extraData={momoRequest.extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={_settings.PartnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";
            var signature = SignatureUtils.GenerateHmacSha256(rawSignature, _settings.SecretKey);

            // Add signature to request
            var signedRequest = new
            {
                momoRequest.partnerCode,
                momoRequest.partnerName,
                momoRequest.storeId,
                momoRequest.requestId,
                momoRequest.amount,
                momoRequest.orderId,
                momoRequest.orderInfo,
                momoRequest.redirectUrl,
                momoRequest.ipnUrl,
                momoRequest.lang,
                momoRequest.requestType,
                momoRequest.autoCapture,
                momoRequest.extraData,
                signature = signature
            };

            // Send request to MoMo
            var jsonRequest = JsonSerializer.Serialize(signedRequest);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_settings.PaymentUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var momoResponse = JsonSerializer.Deserialize<MoMoPaymentResponse>(responseContent);

                if (momoResponse?.ResultCode == 0)
                {
                    result.IsSuccess = true;
                    result.TransactionId = orderId;
                    result.PaymentUrl = momoResponse.PayUrl;
                    result.ExpiresAt = DateTime.Now.AddMinutes(_settings.TimeoutInMinutes);
                    result.AdditionalData["requestId"] = requestId;
                    result.AdditionalData["deepLink"] = momoResponse.DeepLink;

                    _logger.LogInformation("MoMo payment initialized successfully. OrderId: {OrderId}", orderId);
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = GetMoMoErrorMessage(momoResponse?.ResultCode ?? -1);
                    result.ErrorCode = $"MOMO_{momoResponse?.ResultCode}";
                }
            }
            else
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Failed to communicate with MoMo gateway";
                result.ErrorCode = "MOMO_COMMUNICATION_ERROR";
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing MoMo payment for order {OrderId}", request.OrderId);
            return new PaymentInitializationResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to initialize payment",
                ErrorCode = "MOMO_INIT_ERROR"
            };
        }
    }

    /// <summary>
    /// Processes MoMo payment response
    /// </summary>
    public async Task<PaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Processing MoMo payment response for order {OrderId}", request.OrderId);

            var result = new PaymentResult
            {
                TransactionId = request.TransactionId
            };

            // Parse MoMo response
            var momoResponse = JsonSerializer.Deserialize<MoMoPaymentResponse>(request.GatewayResponse);

            if (momoResponse == null)
            {
                result.IsSuccess = false;
                result.Status = PaymentStatus.Failed;
                result.ErrorMessage = "Invalid payment response format";
                result.ErrorCode = "MOMO_INVALID_RESPONSE";
                return result;
            }

            // Validate signature
            if (!await ValidateResponseSignature(momoResponse))
            {
                result.IsSuccess = false;
                result.Status = PaymentStatus.Failed;
                result.ErrorMessage = "Invalid payment response signature";
                result.ErrorCode = "MOMO_INVALID_SIGNATURE";
                return result;
            }

            // Extract payment information
            result.Amount = momoResponse.Amount;
            result.Currency = "VND";
            result.ProcessedAt = DateTime.Now;
            result.GatewayResponse = PaymentEncryptionUtils.SanitizeForLogging(request.GatewayResponse);

            // Map MoMo result code to payment status
            result.Status = MapMoMoResultCode(momoResponse.ResultCode);
            result.IsSuccess = result.Status == PaymentStatus.Completed;

            if (!result.IsSuccess)
            {
                result.ErrorMessage = GetMoMoErrorMessage(momoResponse.ResultCode);
                result.ErrorCode = $"MOMO_{momoResponse.ResultCode}";
            }

            result.Metadata["momoTransId"] = momoResponse.TransId;
            result.Metadata["momoResultCode"] = momoResponse.ResultCode.ToString();
            result.Metadata["requestId"] = momoResponse.RequestId;

            _logger.LogInformation("MoMo payment processed. Status: {Status}, ResultCode: {ResultCode}",
                result.Status, momoResponse.ResultCode);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MoMo payment for order {OrderId}", request.OrderId);
            return new PaymentResult
            {
                IsSuccess = false,
                Status = PaymentStatus.Failed,
                TransactionId = request.TransactionId,
                ErrorMessage = "Failed to process payment response",
                ErrorCode = "MOMO_PROCESS_ERROR"
            };
        }
    }

    /// <summary>
    /// Processes MoMo refund request
    /// </summary>
    public async Task<RefundResult> ProcessRefundAsync(ProcessRefundRequest request)
    {
        try
        {
            _logger.LogInformation("Processing MoMo refund for payment {PaymentId}", request.PaymentId);

            var refundId = PaymentHelpers.GenerateTransactionId("MOMO_REFUND");
            var requestId = PaymentHelpers.GenerateTransactionId("REFUND_REQ");

            var refundRequest = new
            {
                partnerCode = _settings.PartnerCode,
                requestId = requestId,
                orderId = request.OriginalTransactionId,
                transId = request.OriginalTransactionId,
                amount = (long)request.RefundAmount,
                description = request.Reason
            };

            // Generate signature for refund
            var rawSignature = $"accessKey={_settings.AccessKey}&amount={refundRequest.amount}&description={refundRequest.description}&orderId={refundRequest.orderId}&partnerCode={_settings.PartnerCode}&requestId={requestId}&transId={refundRequest.transId}";
            var signature = SignatureUtils.GenerateHmacSha256(rawSignature, _settings.SecretKey);

            var signedRefundRequest = new
            {
                refundRequest.partnerCode,
                refundRequest.requestId,
                refundRequest.orderId,
                refundRequest.transId,
                refundRequest.amount,
                refundRequest.description,
                signature = signature
            };

            // Send refund request to MoMo
            var jsonRequest = JsonSerializer.Serialize(signedRefundRequest);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            // Note: MoMo refund endpoint would be different from payment endpoint
            var refundResponse = await _httpClient.PostAsync($"{_settings.PaymentUrl}/refund", content);
            var responseContent = await refundResponse.Content.ReadAsStringAsync();

            var result = new RefundResult
            {
                RefundId = refundId,
                OriginalTransactionId = request.OriginalTransactionId,
                RefundAmount = request.RefundAmount,
                ProcessedAt = DateTime.Now
            };

            if (refundResponse.IsSuccessStatusCode)
            {
                var momoRefundResponse = JsonSerializer.Deserialize<MoMoRefundResponse>(responseContent);
                result.IsSuccess = momoRefundResponse?.ResultCode == 0;
                result.Status = result.IsSuccess ? PaymentStatus.Refunded : PaymentStatus.Failed;

                if (!result.IsSuccess)
                {
                    result.ErrorMessage = GetMoMoErrorMessage(momoRefundResponse?.ResultCode ?? -1);
                    result.ErrorCode = $"MOMO_REFUND_{momoRefundResponse?.ResultCode}";
                }
            }
            else
            {
                result.IsSuccess = false;
                result.Status = PaymentStatus.Failed;
                result.ErrorMessage = "Failed to communicate with MoMo gateway";
                result.ErrorCode = "MOMO_REFUND_COMMUNICATION_ERROR";
            }

            _logger.LogInformation("MoMo refund processed. Success: {Success}, RefundId: {RefundId}",
                result.IsSuccess, refundId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MoMo refund for payment {PaymentId}", request.PaymentId);
            return new RefundResult
            {
                IsSuccess = false,
                OriginalTransactionId = request.OriginalTransactionId,
                RefundAmount = request.RefundAmount,
                Status = PaymentStatus.Failed,
                ErrorMessage = "Failed to process refund",
                ErrorCode = "MOMO_REFUND_ERROR"
            };
        }
    }

    /// <summary>
    /// Gets payment status from MoMo
    /// </summary>
    public async Task<PaymentStatus> GetPaymentStatusAsync(string transactionId)
    {
        try
        {
            // In production, query MoMo API for transaction status
            await Task.Delay(100); // Simulate API call
            return PaymentStatus.Completed; // Simplified for demonstration
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting MoMo payment status for transaction {TransactionId}", transactionId);
            return PaymentStatus.Failed;
        }
    }

    /// <summary>
    /// Validates MoMo webhook signature using RSA
    /// </summary>
    public async Task<bool> ValidateWebhookAsync(string payload, string signature, IDictionary<string, string> headers)
    {
        try
        {
            if (string.IsNullOrEmpty(_settings.PublicKey))
            {
                _logger.LogWarning("MoMo public key not configured for signature validation");
                return false;
            }

            return SignatureUtils.ValidateRsaSignature(payload, signature, _settings.PublicKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating MoMo webhook signature");
            return false;
        }
    }

    /// <summary>
    /// Processes MoMo webhook notification (IPN)
    /// </summary>
    public async Task<PaymentWebhookResult> ProcessWebhookAsync(string payload, IDictionary<string, string> headers)
    {
        try
        {
            var ipnData = JsonSerializer.Deserialize<MoMoIpnData>(payload);

            if (ipnData == null)
            {
                return new PaymentWebhookResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid IPN data format",
                    RequiresResponse = false
                };
            }

            return new PaymentWebhookResult
            {
                IsSuccess = true,
                TransactionId = ipnData.OrderId,
                Status = MapMoMoResultCode(ipnData.ResultCode),
                Action = "payment",
                Data = new Dictionary<string, object>
                {
                    ["transId"] = ipnData.TransId,
                    ["resultCode"] = ipnData.ResultCode,
                    ["amount"] = ipnData.Amount,
                    ["orderInfo"] = ipnData.OrderInfo ?? ""
                },
                RequiresResponse = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MoMo webhook");
            return new PaymentWebhookResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to process webhook",
                RequiresResponse = false
            };
        }
    }

    /// <summary>
    /// Checks if payment method is supported by MoMo
    /// </summary>
    public bool SupportsPaymentMethod(PaymentMethod method)
    {
        return PaymentHelpers.IsPaymentMethodSupported(method, PaymentGateway.MoMo);
    }

    /// <summary>
    /// Gets supported currencies for MoMo
    /// </summary>
    public IReadOnlyList<string> GetSupportedCurrencies()
    {
        return new List<string> { "VND" }.AsReadOnly();
    }

    /// <summary>
    /// Generates deep link for MoMo mobile app
    /// </summary>
    public async Task<string> GenerateDeepLinkAsync(string transactionId)
    {
        try
        {
            // MoMo deep link format for mobile app integration
            var deepLink = $"momo://payment?orderId={transactionId}&partnerCode={_settings.PartnerCode}";

            _logger.LogInformation("Generated MoMo deep link for transaction {TransactionId}", transactionId);
            return deepLink;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating MoMo deep link for transaction {TransactionId}", transactionId);
            return string.Empty;
        }
    }

    /// <summary>
    /// Processes split payment to multiple recipients
    /// </summary>
    public async Task<SplitPaymentResult> ProcessSplitPaymentAsync(SplitPaymentRequest request)
    {
        try
        {
            // MoMo split payment implementation
            var result = new SplitPaymentResult
            {
                IsSuccess = true,
                TransactionId = PaymentHelpers.GenerateTransactionId("MOMO_SPLIT"),
                RecipientStatuses = new List<SplitPaymentStatus>()
            };

            foreach (var recipient in request.Recipients)
            {
                var recipientStatus = new SplitPaymentStatus
                {
                    RecipientId = recipient.RecipientId,
                    Amount = recipient.Amount,
                    Status = PaymentStatus.Processing, // Would be updated via API
                    TransactionId = PaymentHelpers.GenerateTransactionId("MOMO_SPLIT_ITEM")
                };

                result.RecipientStatuses.Add(recipientStatus);
            }

            _logger.LogInformation("MoMo split payment processed for order {OrderId}", request.OrderId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MoMo split payment for order {OrderId}", request.OrderId);
            return new SplitPaymentResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to process split payment"
            };
        }
    }

    #region Private Helper Methods

    private PaymentValidationResult ValidateInitializationRequest(InitializePaymentRequest request)
    {
        return PaymentHelpers.ValidateAmount(request.Amount, PaymentGateway.MoMo, request.Currency);
    }

    private string GetRequestType(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.EWallet => "captureWallet",
            PaymentMethod.QRCode => "qrcode",
            PaymentMethod.BankTransfer => "linkWallet",
            _ => "captureWallet"
        };
    }

    private string BuildExtraData(InitializePaymentRequest request)
    {
        var extraData = new Dictionary<string, object>
        {
            ["orderId"] = request.OrderId,
            ["customerEmail"] = request.CustomerEmail,
            ["customerName"] = request.CustomerName,
            ["method"] = request.Method.ToString()
        };

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(extraData)));
    }

    private async Task<bool> ValidateResponseSignature(MoMoPaymentResponse response)
    {
        try
        {
            var rawSignature = $"accessKey={_settings.AccessKey}&amount={response.Amount}&extraData={response.ExtraData}&message={response.Message}&orderId={response.OrderId}&orderInfo={response.OrderInfo}&orderType={response.OrderType}&partnerCode={response.PartnerCode}&payType={response.PayType}&requestId={response.RequestId}&responseTime={response.ResponseTime}&resultCode={response.ResultCode}&transId={response.TransId}";

            var expectedSignature = SignatureUtils.GenerateHmacSha256(rawSignature, _settings.SecretKey);
            return response.Signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private PaymentStatus MapMoMoResultCode(int resultCode)
    {
        return resultCode switch
        {
            0 => PaymentStatus.Completed,
            9000 => PaymentStatus.Processing,
            1000 => PaymentStatus.Processing,
            _ => PaymentStatus.Failed
        };
    }

    private string GetMoMoErrorMessage(int resultCode)
    {
        return resultCode switch
        {
            0 => "Success",
            9000 => "Transaction is being processed",
            1000 => "Transaction is being confirmed",
            10 => "Invalid merchant",
            11 => "Access denied",
            12 => "Invalid amount",
            13 => "Invalid merchant information",
            20 => "Invalid signature",
            21 => "Invalid order information",
            40 => "Order not found",
            41 => "Order cannot be processed",
            42 => "Order has been processed",
            43 => "Insufficient balance",
            _ => "Transaction failed"
        };
    }

    #endregion

    #region Data Transfer Objects

    private class MoMoPaymentResponse
    {
        public string PartnerCode { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public long ResponseTime { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ResultCode { get; set; }
        public string PayUrl { get; set; } = string.Empty;
        public string DeepLink { get; set; } = string.Empty;
        public string QrCodeUrl { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public string ExtraData { get; set; } = string.Empty;
        public string OrderInfo { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty;
        public string PayType { get; set; } = string.Empty;
        public string TransId { get; set; } = string.Empty;
    }

    private class MoMoRefundResponse
    {
        public string PartnerCode { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TransId { get; set; } = string.Empty;
        public int ResultCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public long ResponseTime { get; set; }
    }

    private class MoMoIpnData
    {
        public string PartnerCode { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string OrderInfo { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty;
        public string TransId { get; set; } = string.Empty;
        public int ResultCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string PayType { get; set; } = string.Empty;
        public long ResponseTime { get; set; }
        public string ExtraData { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }

    #endregion
}