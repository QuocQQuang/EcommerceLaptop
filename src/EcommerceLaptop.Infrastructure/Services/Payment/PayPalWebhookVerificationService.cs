using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EcommerceLaptop.Core.Configuration;
using System.IO.Hashing;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace EcommerceLaptop.Infrastructure.Services.Payment;

/// <summary>
/// PayPal webhook signature verification service
/// Implements PayPal's recommended webhook verification method
/// Reference: https://developer.paypal.com/api/rest/webhooks/rest/
/// </summary>
public class PayPalWebhookVerificationService
{
    private readonly PayPalSettings _settings;
    private readonly ILogger<PayPalWebhookVerificationService> _logger;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, X509Certificate2> _certificateCache;

    public PayPalWebhookVerificationService(
        IOptions<PaymentGatewaySettings> settings,
        ILogger<PayPalWebhookVerificationService> logger,
        HttpClient httpClient)
    {
        _settings = settings.Value.PayPal;
        _logger = logger;
        _httpClient = httpClient;
        _certificateCache = new Dictionary<string, X509Certificate2>();
    }

    /// <summary>
    /// Verifies PayPal webhook signature using self-verification method
    /// </summary>
    /// <param name="payload">Raw webhook payload</param>
    /// <param name="headers">HTTP headers from webhook request</param>
    /// <returns>True if signature is valid</returns>
    public async Task<bool> VerifyWebhookSignatureAsync(string payload, IDictionary<string, string> headers)
    {
        try
        {
            // Extract required headers
            if (!TryExtractHeaders(headers, out var transmissionId, out var timeStamp,
                out var certUrl, out var transmissionSig, out var authAlgo))
            {
                _logger.LogWarning("PayPal webhook missing required headers");
                return false;
            }

            // Validate webhook age (PayPal recommends checking timestamp)
            if (!ValidateWebhookTimestamp(timeStamp))
            {
                _logger.LogWarning("PayPal webhook timestamp is too old or invalid: {TimeStamp}", timeStamp);
                return false;
            }

            // Download and cache certificate
            var certificate = await GetCertificateAsync(certUrl);
            if (certificate == null)
            {
                _logger.LogError("Failed to download PayPal certificate from: {CertUrl}", certUrl);
                return false;
            }

            // Calculate CRC32 of payload
            var crc32Value = CalculateCrc32(Encoding.UTF8.GetBytes(payload));

            // Form the original message for signature verification
            var originalMessage = $"{transmissionId}|{timeStamp}|{_settings.WebhookId}|{crc32Value}";

            _logger.LogDebug("PayPal webhook original message: {Message}", originalMessage);

            // Verify signature
            var isValid = VerifySignature(originalMessage, transmissionSig, certificate, authAlgo);

            _logger.LogInformation("PayPal webhook signature verification result: {IsValid}", isValid);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying PayPal webhook signature");
            return false;
        }
    }

    /// <summary>
    /// Alternative verification method using PayPal's verify-webhook-signature API
    /// </summary>
    /// <param name="payload">Raw webhook payload</param>
    /// <param name="headers">HTTP headers from webhook request</param>
    /// <returns>True if signature is valid</returns>
    public async Task<bool> VerifyWebhookSignatureViaApiAsync(string payload, IDictionary<string, string> headers)
    {
        try
        {
            // Extract required headers
            if (!TryExtractHeaders(headers, out var transmissionId, out var timeStamp,
                out var certUrl, out var transmissionSig, out var authAlgo))
            {
                _logger.LogWarning("PayPal webhook missing required headers for API verification");
                return false;
            }

            // Parse webhook event to get event data
            var webhookEvent = JsonSerializer.Deserialize<JsonElement>(payload);

            var verificationRequest = new
            {
                transmission_id = transmissionId,
                transmission_time = timeStamp,
                cert_url = certUrl,
                auth_algo = authAlgo,
                transmission_sig = transmissionSig,
                webhook_id = _settings.WebhookId,
                webhook_event = webhookEvent
            };

            var requestJson = JsonSerializer.Serialize(verificationRequest);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            // Get access token for API call
            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("Failed to get PayPal access token for webhook verification");
                return false;
            }

            // Call PayPal verification API
            var apiUrl = GetApiBaseUrl() + "/v1/notifications/verify-webhook-signature";
            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = content,
                Headers = { { "Authorization", $"Bearer {accessToken}" } }
            };

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                var verificationStatus = result.TryGetProperty("verification_status", out var status)
                    ? status.GetString() : null;

                var isValid = verificationStatus == "SUCCESS";

                _logger.LogInformation("PayPal API webhook verification result: {Status}", verificationStatus);
                return isValid;
            }
            else
            {
                _logger.LogError("PayPal webhook verification API returned error: {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying PayPal webhook via API");
            return false;
        }
    }

    private bool TryExtractHeaders(IDictionary<string, string> headers,
        out string transmissionId, out string timeStamp, out string certUrl,
        out string transmissionSig, out string authAlgo)
    {
        transmissionId = GetHeaderValue(headers, "paypal-transmission-id");
        timeStamp = GetHeaderValue(headers, "paypal-transmission-time");
        certUrl = GetHeaderValue(headers, "paypal-cert-url");
        transmissionSig = GetHeaderValue(headers, "paypal-transmission-sig");
        authAlgo = GetHeaderValue(headers, "paypal-auth-algo");

        return !string.IsNullOrEmpty(transmissionId) &&
               !string.IsNullOrEmpty(timeStamp) &&
               !string.IsNullOrEmpty(certUrl) &&
               !string.IsNullOrEmpty(transmissionSig) &&
               !string.IsNullOrEmpty(authAlgo);
    }

    private string GetHeaderValue(IDictionary<string, string> headers, string key)
    {
        // Try exact match first
        if (headers.TryGetValue(key, out var value))
            return value;

        // Try case-insensitive match
        var kvp = headers.FirstOrDefault(h =>
            string.Equals(h.Key, key, StringComparison.OrdinalIgnoreCase));

        return kvp.Key != null ? kvp.Value : string.Empty;
    }

    private bool ValidateWebhookTimestamp(string timeStamp)
    {
        if (!DateTime.TryParse(timeStamp, out var webhookTime))
            return false;

        // PayPal recommends rejecting webhooks older than a certain time (e.g., 5 minutes)
        var maxAge = TimeSpan.FromMinutes(5);
        var age = DateTime.UtcNow - webhookTime;

        return age <= maxAge;
    }

    private async Task<X509Certificate2?> GetCertificateAsync(string certUrl)
    {
        try
        {
            // Check cache first
            if (_certificateCache.TryGetValue(certUrl, out var cachedCert))
            {
                // Check if certificate is still valid
                if (cachedCert.NotAfter > DateTime.UtcNow)
                    return cachedCert;

                // Remove expired certificate
                _certificateCache.Remove(certUrl);
            }

            // Download certificate
            var certData = await _httpClient.GetStringAsync(certUrl);
            var certificate = X509CertificateLoader.LoadCertificate(Encoding.UTF8.GetBytes(certData));

            // Cache certificate
            _certificateCache[certUrl] = certificate;

            return certificate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading PayPal certificate from {CertUrl}", certUrl);
            return null;
        }
    }

    private uint CalculateCrc32(byte[] data)
    {
        var crc32 = new Crc32();
        crc32.Append(data);
        var hash = crc32.GetHashAndReset();

        // Convert to unsigned 32-bit integer in decimal form
        return BitConverter.ToUInt32(hash, 0);
    }

    private bool VerifySignature(string originalMessage, string signature, X509Certificate2 certificate, string authAlgo)
    {
        try
        {
            var signatureBytes = Convert.FromBase64String(signature);
            var messageBytes = Encoding.UTF8.GetBytes(originalMessage);

            using var rsa = certificate.GetRSAPublicKey();
            if (rsa == null)
            {
                _logger.LogError("Failed to extract RSA public key from PayPal certificate");
                return false;
            }

            // PayPal uses SHA256withRSA
            var hashAlgorithm = authAlgo switch
            {
                "SHA256withRSA" => HashAlgorithmName.SHA256,
                "SHA1withRSA" => HashAlgorithmName.SHA1,
                _ => HashAlgorithmName.SHA256
            };

            return rsa.VerifyData(messageBytes, signatureBytes, hashAlgorithm, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying RSA signature");
            return false;
        }
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            var authString = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, GetApiBaseUrl() + "/v1/oauth2/token")
            {
                Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded"),
                Headers = { { "Authorization", $"Basic {authString}" } }
            };

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                return tokenResponse.TryGetProperty("access_token", out var token)
                    ? token.GetString() : null;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PayPal access token");
            return null;
        }
    }

    private string GetApiBaseUrl()
    {
        return _settings.Environment.ToLower() == "live"
            ? "https://api.paypal.com"
            : "https://api.sandbox.paypal.com";
    }
}