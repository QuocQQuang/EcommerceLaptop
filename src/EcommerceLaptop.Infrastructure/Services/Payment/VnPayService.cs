using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.Services.Payment;
using EcommerceLaptop.Core.DTOs.Payment;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Configuration;
using EcommerceLaptop.Core.Utilities.Payment;
using System.Text;
using System.Web;

namespace EcommerceLaptop.Infrastructure.Services.Payment;

/// <summary>
/// VnPay payment service implementation following Vietnamese payment regulations
/// Implements secure HMAC-SHA512 signature validation and QR code generation
/// </summary>
public class VnPayService : IVnPayService
{
    private readonly VnPaySettings _settings;
    private readonly ILogger<VnPayService> _logger;
    private readonly HttpClient _httpClient;

    public PaymentGateway Gateway => PaymentGateway.VnPay;

    public VnPayService(IOptions<PaymentGatewaySettings> settings, ILogger<VnPayService> logger, HttpClient httpClient)
    {
        _settings = settings.Value.VnPay;
        _logger = logger;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Initializes VnPay payment with HMAC-SHA512 signature
    /// </summary>
    public async Task<PaymentInitializationResult> InitializePaymentAsync(InitializePaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Initializing VnPay payment for order {OrderId}", request.OrderId);

            var result = new PaymentInitializationResult();

            // Validate request
            var validation = ValidateInitializationRequest(request);
            if (!validation.IsValid)
            {
                result.IsSuccess = false;
                result.ErrorMessage = string.Join(", ", validation.ValidationErrors);
                return result;
            }

            // Generate transaction ID
            var txnRef = PaymentHelpers.GenerateTransactionId("VNP");
            var createDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            var expireDate = DateTime.Now.AddMinutes(_settings.TimeoutInMinutes).ToString("yyyyMMddHHmmss");

            // Build VnPay parameters
            var vnpParams = new SortedDictionary<string, string>
            {
                ["vnp_Version"] = _settings.Version,
                ["vnp_Command"] = _settings.Command,
                ["vnp_TmnCode"] = _settings.TmnCode,
                ["vnp_Amount"] = ((long)(request.Amount * 100)).ToString(), // VnPay requires amount in VND cents
                ["vnp_CurrCode"] = _settings.CurrCode,
                ["vnp_TxnRef"] = txnRef,
                ["vnp_OrderInfo"] = $"Payment for order {request.OrderId}",
                ["vnp_OrderType"] = GetOrderType(request.Method),
                ["vnp_Locale"] = _settings.Locale,
                ["vnp_ReturnUrl"] = !string.IsNullOrEmpty(request.ReturnUrl) ? request.ReturnUrl : _settings.ReturnUrl,
                ["vnp_IpAddr"] = "127.0.0.1", // Should be passed from request context
                ["vnp_CreateDate"] = createDate,
                ["vnp_ExpireDate"] = expireDate
            };

            // Add optional parameters
            if (!string.IsNullOrEmpty(request.CustomerEmail))
                vnpParams["vnp_Bill_Email"] = request.CustomerEmail;

            if (!string.IsNullOrEmpty(request.CustomerPhone))
                vnpParams["vnp_Bill_Mobile"] = request.CustomerPhone;

            // Add installment parameters if applicable
            if (request.Method == PaymentMethod.Installment && request.GatewaySpecificData.ContainsKey("installmentMonths"))
            {
                vnpParams["vnp_OrderCategory"] = "installment";
                vnpParams["vnp_InstallmentType"] = "03"; // Individual installment
            }

            // Generate signature
            var signData = string.Join("&", vnpParams.Select(kv => $"{kv.Key}={kv.Value}"));
            var signature = SignatureUtils.GenerateHmacSha512(signData, _settings.HashSecret);
            vnpParams["vnp_SecureHash"] = signature;

            // Build payment URL
            var paymentUrl = BuildPaymentUrl(vnpParams);

            result.IsSuccess = true;
            result.TransactionId = txnRef;
            result.PaymentUrl = paymentUrl;
            result.ExpiresAt = DateTime.Now.AddMinutes(_settings.TimeoutInMinutes);

            // Generate QR code for mobile payments
            if (request.Method == PaymentMethod.QRCode)
            {
                var qrResult = await GenerateQrCodeAsync(txnRef);
                if (qrResult.IsSuccess)
                {
                    result.QrCodeData = qrResult.QrCodeData;
                    result.AdditionalData["qrCodeImageUrl"] = qrResult.QrCodeImageUrl;
                }
            }

            _logger.LogInformation("VnPay payment initialized successfully. TxnRef: {TxnRef}", txnRef);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing VnPay payment for order {OrderId}", request.OrderId);
            return new PaymentInitializationResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to initialize payment",
                ErrorCode = "VNPAY_INIT_ERROR"
            };
        }
    }

    /// <summary>
    /// Processes VnPay payment response
    /// </summary>
    public Task<PaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Processing VnPay payment response for order {OrderId}", request.OrderId);

            var result = new PaymentResult
            {
                TransactionId = request.TransactionId
            };

            // Parse VnPay response
            var vnpParams = ParseGatewayResponse(request.GatewayResponse);

            // Validate signature
            if (!ValidateResponseSignature(vnpParams))
            {
                result.IsSuccess = false;
                result.Status = PaymentStatus.Failed;
                result.ErrorMessage = "Invalid payment response signature";
                result.ErrorCode = "VNPAY_INVALID_SIGNATURE";
                return Task.FromResult(result);
            }

            // Extract payment information
            var responseCode = vnpParams.GetValueOrDefault("vnp_ResponseCode", "");
            var transactionNo = vnpParams.GetValueOrDefault("vnp_TransactionNo", "");
            var amount = decimal.Parse(vnpParams.GetValueOrDefault("vnp_Amount", "0")) / 100; // Convert from cents

            result.Amount = amount;
            result.Currency = "VND";
            result.ProcessedAt = DateTime.Now;
            result.GatewayResponse = PaymentEncryptionUtils.SanitizeForLogging(request.GatewayResponse);

            // Map VnPay response code to payment status
            result.Status = MapVnPayResponseCode(responseCode);
            result.IsSuccess = result.Status == PaymentStatus.Completed;

            if (!result.IsSuccess)
            {
                result.ErrorMessage = GetVnPayErrorMessage(responseCode);
                result.ErrorCode = $"VNPAY_{responseCode}";
            }

            result.Metadata["vnpTransactionNo"] = transactionNo;
            result.Metadata["vnpResponseCode"] = responseCode;

            _logger.LogInformation("VnPay payment processed. Status: {Status}, ResponseCode: {ResponseCode}",
                result.Status, responseCode);

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VnPay payment for order {OrderId}", request.OrderId);
            return Task.FromResult(new PaymentResult
            {
                IsSuccess = false,
                Status = PaymentStatus.Failed,
                TransactionId = request.TransactionId,
                ErrorMessage = "Failed to process payment response",
                ErrorCode = "VNPAY_PROCESS_ERROR"
            });
        }
    }

    /// <summary>
    /// Processes VnPay refund request
    /// </summary>
    public async Task<RefundResult> ProcessRefundAsync(ProcessRefundRequest request)
    {
        try
        {
            _logger.LogInformation("Processing VnPay refund for payment {PaymentId}", request.PaymentId);

            // VnPay refund implementation would require calling their refund API
            // This is a simplified implementation for demonstration
            var result = new RefundResult
            {
                RefundId = PaymentHelpers.GenerateTransactionId("VNP_REFUND"),
                OriginalTransactionId = request.OriginalTransactionId,
                RefundAmount = request.RefundAmount,
                Status = PaymentStatus.Processing, // Would be updated via webhook
                ProcessedAt = DateTime.Now,
                IsSuccess = true
            };

            // In production, make actual API call to VnPay refund endpoint
            await Task.Delay(100); // Simulate API call

            _logger.LogInformation("VnPay refund initiated. RefundId: {RefundId}", result.RefundId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VnPay refund for payment {PaymentId}", request.PaymentId);
            return new RefundResult
            {
                IsSuccess = false,
                OriginalTransactionId = request.OriginalTransactionId,
                RefundAmount = request.RefundAmount,
                Status = PaymentStatus.Failed,
                ErrorMessage = "Failed to process refund",
                ErrorCode = "VNPAY_REFUND_ERROR"
            };
        }
    }

    /// <summary>
    /// Gets payment status from VnPay
    /// </summary>
    public async Task<PaymentStatus> GetPaymentStatusAsync(string transactionId)
    {
        try
        {
            // In production, query VnPay API for transaction status
            await Task.Delay(100); // Simulate API call
            return PaymentStatus.Completed; // Simplified for demonstration
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting VnPay payment status for transaction {TransactionId}", transactionId);
            return PaymentStatus.Failed;
        }
    }

    /// <summary>
    /// Validates VnPay webhook signature
    /// </summary>
    public Task<bool> ValidateWebhookAsync(string payload, string signature, IDictionary<string, string> headers)
    {
        try
        {
            var vnpParams = ParseGatewayResponse(payload);
            var isValid = ValidateResponseSignature(vnpParams);
            return Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating VnPay webhook signature");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Processes VnPay webhook notification with proper signature validation
    /// Follows official VnPay IPN implementation patterns
    /// </summary>
    public Task<PaymentWebhookResult> ProcessWebhookAsync(string payload, IDictionary<string, string> headers)
    {
        try
        {
            _logger.LogInformation("Processing VnPay webhook notification");

            // Parse VnPay parameters from payload (query string format)
            var vnpParams = ParseGatewayResponse(payload);
            
            // Extract required VnPay parameters
            var transactionId = vnpParams.GetValueOrDefault("vnp_TxnRef", "");
            var responseCode = vnpParams.GetValueOrDefault("vnp_ResponseCode", "");
            var transactionStatus = vnpParams.GetValueOrDefault("vnp_TransactionStatus", "");
            var vnpSecureHash = vnpParams.GetValueOrDefault("vnp_SecureHash", "");
            var vnpAmount = vnpParams.GetValueOrDefault("vnp_Amount", "0");
            var vnpTransactionNo = vnpParams.GetValueOrDefault("vnp_TransactionNo", "");

            _logger.LogInformation("VnPay webhook - TxnRef: {TxnRef}, ResponseCode: {ResponseCode}, TransactionStatus: {TransactionStatus}", 
                transactionId, responseCode, transactionStatus);

            // Validate signature first (security requirement)
            if (!ValidateWebhookSignature(vnpParams, vnpSecureHash))
            {
                _logger.LogWarning("VnPay webhook signature validation failed for transaction {TransactionId}", transactionId);
                return Task.FromResult(new PaymentWebhookResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid signature",
                    RequiresResponse = true,
                    ResponseContent = "{\"RspCode\":\"97\",\"Message\":\"Invalid signature\"}"
                });
            }

            _logger.LogInformation("VnPay webhook signature validation successful for transaction {TransactionId}", transactionId);

            // Parse amount (VnPay sends amount * 100)
            if (!long.TryParse(vnpAmount, out var amountInCents))
            {
                _logger.LogWarning("VnPay webhook - invalid amount format: {Amount}", vnpAmount);
                return Task.FromResult(new PaymentWebhookResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid amount format",
                    RequiresResponse = true,
                    ResponseContent = "{\"RspCode\":\"04\",\"Message\":\"Invalid amount\"}"
                });
            }

            var actualAmount = amountInCents / 100m; // Convert back to actual amount

            // Check transaction status and response code
            var paymentStatus = MapVnPayResponseCode(responseCode);
            var isSuccessful = responseCode == "00" && transactionStatus == "00";

            var result = new PaymentWebhookResult
            {
                IsSuccess = true,
                TransactionId = transactionId,
                Status = paymentStatus,
                Action = "payment_notification",
                Data = new Dictionary<string, object>
                {
                    ["vnp_TxnRef"] = transactionId,
                    ["vnp_ResponseCode"] = responseCode,
                    ["vnp_TransactionStatus"] = transactionStatus,
                    ["vnp_Amount"] = actualAmount,
                    ["vnp_TransactionNo"] = vnpTransactionNo,
                    ["isSuccessful"] = isSuccessful,
                    ["gateway"] = "VnPay"
                },
                RequiresResponse = true,
                ResponseContent = "{\"RspCode\":\"00\",\"Message\":\"Confirm Success\"}"
            };

            if (isSuccessful)
            {
                _logger.LogInformation("VnPay payment successful - OrderId: {OrderId}, VnPay TranId: {VnPayTranId}, Amount: {Amount}", 
                    transactionId, vnpTransactionNo, actualAmount);
            }
            else
            {
                _logger.LogWarning("VnPay payment failed - OrderId: {OrderId}, ResponseCode: {ResponseCode}, TransactionStatus: {TransactionStatus}", 
                    transactionId, responseCode, transactionStatus);
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VnPay webhook");
            return Task.FromResult(new PaymentWebhookResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to process webhook",
                RequiresResponse = true,
                ResponseContent = "{\"RspCode\":\"99\",\"Message\":\"Input data required\"}"
            });
        }
    }

    /// <summary>
    /// Checks if payment method is supported by VnPay
    /// </summary>
    public bool SupportsPaymentMethod(PaymentMethod method)
    {
        return PaymentHelpers.IsPaymentMethodSupported(method, PaymentGateway.VnPay);
    }

    /// <summary>
    /// Gets supported currencies for VnPay
    /// </summary>
    public IReadOnlyList<string> GetSupportedCurrencies()
    {
        return new List<string> { "VND" }.AsReadOnly();
    }

    /// <summary>
    /// Generates QR code for VnPay mobile payments
    /// </summary>
    public async Task<QrCodeResult> GenerateQrCodeAsync(string transactionId)
    {
        try
        {
            // In production, call VnPay QR code generation API
            var qrData = $"vnpay://payment?txnRef={transactionId}";

            return new QrCodeResult
            {
                IsSuccess = true,
                QrCodeData = qrData,
                QrCodeImageUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={Uri.EscapeDataString(qrData)}",
                PaymentUrl = qrData,
                ExpiresAt = DateTime.Now.AddMinutes(_settings.TimeoutInMinutes)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating VnPay QR code for transaction {TransactionId}", transactionId);
            return new QrCodeResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to generate QR code"
            };
        }
    }

    /// <summary>
    /// Processes installment payment request
    /// </summary>
    public async Task<InstallmentResult> ProcessInstallmentAsync(InstallmentPaymentRequest request)
    {
        try
        {
            var monthlyAmount = request.Amount / request.InstallmentMonths;
            var interestRate = GetInstallmentInterestRate(request.InstallmentMonths);

            return new InstallmentResult
            {
                IsSuccess = true,
                TransactionId = PaymentHelpers.GenerateTransactionId("VNP_INSTALL"),
                InstallmentMonths = request.InstallmentMonths,
                MonthlyAmount = monthlyAmount,
                InterestRate = interestRate,
                PaymentUrl = await GenerateInstallmentPaymentUrl(request)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VnPay installment payment");
            return new InstallmentResult
            {
                IsSuccess = false,
                ErrorMessage = "Failed to process installment payment"
            };
        }
    }

    #region Private Helper Methods

    private PaymentValidationResult ValidateInitializationRequest(InitializePaymentRequest request)
    {
        return PaymentHelpers.ValidateAmount(request.Amount, PaymentGateway.VnPay, request.Currency);
    }

    private string GetOrderType(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.CreditCard => "billpayment",
            PaymentMethod.DebitCard => "billpayment",
            PaymentMethod.BankTransfer => "other",
            PaymentMethod.QRCode => "other",
            PaymentMethod.Installment => "installment",
            _ => "other"
        };
    }

    private string BuildPaymentUrl(SortedDictionary<string, string> parameters)
    {
        var queryString = string.Join("&", parameters.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        return $"{_settings.PaymentUrl}?{queryString}";
    }

    private Dictionary<string, string> ParseGatewayResponse(string response)
    {
        var parameters = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(response))
            return parameters;

        // Parse URL query string format
        var pairs = response.Split('&');
        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=', 2);
            if (keyValue.Length == 2)
            {
                parameters[keyValue[0]] = Uri.UnescapeDataString(keyValue[1]);
            }
        }

        return parameters;
    }

    private bool ValidateResponseSignature(Dictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("vnp_SecureHash", out var receivedSignature))
            return false;

        var signParams = parameters
            .Where(kv => kv.Key != "vnp_SecureHash")
            .OrderBy(kv => kv.Key)
            .Select(kv => $"{kv.Key}={kv.Value}");

        var signData = string.Join("&", signParams);
        var expectedSignature = SignatureUtils.GenerateHmacSha512(signData, _settings.HashSecret);

        return SignatureUtils.ValidateHmacSignature(signData, receivedSignature, _settings.HashSecret, "SHA512");
    }

    /// <summary>
    /// Validates VnPay webhook signature following official IPN implementation
    /// Based on VnPay official C# sandbox implementation
    /// </summary>
    private bool ValidateWebhookSignature(Dictionary<string, string> parameters, string receivedSignature)
    {
        if (string.IsNullOrEmpty(receivedSignature))
        {
            _logger.LogWarning("VnPay webhook signature is empty");
            return false;
        }

        try
        {
            // Remove signature and hash type parameters as per VnPay official implementation
            var signData = BuildSignatureData(parameters);
            var expectedSignature = SignatureUtils.GenerateHmacSha512(signData, _settings.HashSecret);

            var isValid = SignatureUtils.ValidateHmacSignature(signData, receivedSignature, _settings.HashSecret, "SHA512");
            
            if (!isValid)
            {
                _logger.LogWarning("VnPay signature mismatch. Expected: {Expected}, Received: {Received}, Data: {Data}", 
                    expectedSignature, receivedSignature, signData);
            }
            else
            {
                _logger.LogDebug("VnPay signature validation successful");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating VnPay webhook signature");
            return false;
        }
    }

    /// <summary>
    /// Builds signature data following official VnPay pattern
    /// Excludes vnp_SecureHash and vnp_SecureHashType, URL encodes parameters
    /// </summary>
    private string BuildSignatureData(Dictionary<string, string> parameters)
    {
        var signParams = parameters
            .Where(kv => !string.IsNullOrEmpty(kv.Value) && 
                        kv.Key != "vnp_SecureHash" && 
                        kv.Key != "vnp_SecureHashType")
            .OrderBy(kv => kv.Key)
            .Select(kv => $"{HttpUtility.UrlEncode(kv.Key)}={HttpUtility.UrlEncode(kv.Value)}");

        return string.Join("&", signParams);
    }

    private PaymentStatus MapVnPayResponseCode(string responseCode)
    {
        return responseCode switch
        {
            "00" => PaymentStatus.Completed,
            "07" => PaymentStatus.Processing,
            "09" => PaymentStatus.Processing,
            "10" => PaymentStatus.Failed,
            "11" => PaymentStatus.Failed,
            "12" => PaymentStatus.Failed,
            "24" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Failed
        };
    }

    private string GetVnPayErrorMessage(string responseCode)
    {
        return responseCode switch
        {
            "07" => "Transaction is being processed",
            "09" => "Transaction failed - customer has not successfully registered for Internet Banking at bank",
            "10" => "Transaction failed - customer has entered incorrect bank account information more than 3 times",
            "11" => "Transaction failed - payment deadline has expired",
            "12" => "Transaction failed - customer account is locked",
            "24" => "Transaction was cancelled by customer",
            "51" => "Transaction failed - insufficient account balance",
            "65" => "Transaction failed - customer has exceeded daily transaction limit",
            _ => "Transaction failed"
        };
    }

    private decimal GetInstallmentInterestRate(int months)
    {
        return months switch
        {
            3 => 0m,
            6 => 2.5m,
            12 => 5.99m,
            18 => 8.99m,
            24 => 11.99m,
            _ => 15.99m
        };
    }

    private async Task<string> GenerateInstallmentPaymentUrl(InstallmentPaymentRequest request)
    {
        // In production, build VnPay installment-specific URL
        await Task.Delay(50);
        return $"{_settings.PaymentUrl}?installment={request.InstallmentMonths}";
    }

    #endregion
}