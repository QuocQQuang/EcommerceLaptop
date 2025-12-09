using System.Security.Cryptography;
using System.Text;

namespace EcommerceLaptop.Core.Utilities.Payment;

/// <summary>
/// Payment signature utilities following security best practices
/// Implements cryptographic operations for payment gateway integrations
/// </summary>
public static class SignatureUtils
{
    /// <summary>
    /// Generates HMAC-SHA512 signature for VnPay
    /// </summary>
    /// <param name="data">Data to sign</param>
    /// <param name="secretKey">Secret key for signing</param>
    /// <returns>HMAC-SHA512 signature in uppercase hex format</returns>
    public static string GenerateHmacSha512(string data, string secretKey)
    {
        if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(secretKey))
            throw new ArgumentException("Data and secret key cannot be null or empty");

        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var dataBytes = Encoding.UTF8.GetBytes(data);

        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Generates HMAC-SHA256 signature for ZaloPay
    /// </summary>
    /// <param name="data">Data to sign</param>
    /// <param name="secretKey">Secret key for signing</param>
    /// <returns>HMAC-SHA256 signature in lowercase hex format</returns>
    public static string GenerateHmacSha256(string data, string secretKey)
    {
        if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(secretKey))
            throw new ArgumentException("Data and secret key cannot be null or empty");

        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var dataBytes = Encoding.UTF8.GetBytes(data);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Validates HMAC signature with timing-safe comparison
    /// </summary>
    /// <param name="data">Original data</param>
    /// <param name="signature">Signature to validate</param>
    /// <param name="secretKey">Secret key</param>
    /// <param name="algorithm">Hash algorithm (SHA256 or SHA512)</param>
    /// <returns>True if signature is valid</returns>
    public static bool ValidateHmacSignature(string data, string signature, string secretKey, string algorithm = "SHA256")
    {
        if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secretKey))
            return false;

        try
        {
            var expectedSignature = algorithm.ToUpperInvariant() switch
            {
                "SHA512" => GenerateHmacSha512(data, secretKey),
                "SHA256" => GenerateHmacSha256(data, secretKey),
                _ => throw new ArgumentException($"Unsupported algorithm: {algorithm}")
            };

            // Timing-safe comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature.ToLowerInvariant()),
                Encoding.UTF8.GetBytes(expectedSignature.ToLowerInvariant())
            );
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates RSA signature for MoMo (placeholder - requires MoMo private key)
    /// </summary>
    /// <param name="data">Data to sign</param>
    /// <param name="privateKey">RSA private key in PEM format</param>
    /// <returns>Base64-encoded RSA signature</returns>
    public static string GenerateRsaSignature(string data, string privateKey)
    {
        if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(privateKey))
            throw new ArgumentException("Data and private key cannot be null or empty");

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKey);
            
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            
            return Convert.ToBase64String(signatureBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to generate RSA signature", ex);
        }
    }

    /// <summary>
    /// Validates RSA signature using public key
    /// </summary>
    /// <param name="data">Original data</param>
    /// <param name="signature">Base64-encoded signature</param>
    /// <param name="publicKey">RSA public key in PEM format</param>
    /// <returns>True if signature is valid</returns>
    public static bool ValidateRsaSignature(string data, string signature, string publicKey)
    {
        if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(publicKey))
            return false;

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKey);
            
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = Convert.FromBase64String(signature);
            
            return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates MD5 hash (for legacy systems only - not recommended for security)
    /// </summary>
    /// <param name="data">Data to hash</param>
    /// <returns>MD5 hash in lowercase hex format</returns>
    [Obsolete("MD5 is cryptographically weak. Use SHA256 or SHA512 instead.")]
    public static string GenerateMd5Hash(string data)
    {
        if (string.IsNullOrEmpty(data))
            return string.Empty;

        using var md5 = MD5.Create();
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var hashBytes = md5.ComputeHash(dataBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

/// <summary>
/// Payment data encryption utilities for sensitive information
/// </summary>
public static class PaymentEncryptionUtils
{
    /// <summary>
    /// Encrypts sensitive payment data using AES-256-GCM
    /// </summary>
    /// <param name="data">Data to encrypt</param>
    /// <param name="key">256-bit encryption key</param>
    /// <returns>Base64-encoded encrypted data with IV prepended</returns>
    public static string EncryptAesGcm(string data, string key)
    {
        if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(key))
            throw new ArgumentException("Data and key cannot be null or empty");

        var keyBytes = Convert.FromBase64String(key);
        if (keyBytes.Length != 32) // 256 bits
            throw new ArgumentException("Key must be 256 bits (32 bytes)");

        var dataBytes = Encoding.UTF8.GetBytes(data);
        var iv = new byte[12]; // 96-bit IV for GCM
        var ciphertext = new byte[dataBytes.Length];
        var tag = new byte[16]; // 128-bit authentication tag

        RandomNumberGenerator.Fill(iv);

        using var aes = new AesGcm(keyBytes, 16);
        aes.Encrypt(iv, dataBytes, ciphertext, tag);

        // Combine IV + ciphertext + tag
        var result = new byte[iv.Length + ciphertext.Length + tag.Length];
        iv.CopyTo(result, 0);
        ciphertext.CopyTo(result, iv.Length);
        tag.CopyTo(result, iv.Length + ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Decrypts AES-256-GCM encrypted data
    /// </summary>
    /// <param name="encryptedData">Base64-encoded encrypted data</param>
    /// <param name="key">256-bit encryption key</param>
    /// <returns>Decrypted plaintext</returns>
    public static string DecryptAesGcm(string encryptedData, string key)
    {
        if (string.IsNullOrEmpty(encryptedData) || string.IsNullOrEmpty(key))
            throw new ArgumentException("Encrypted data and key cannot be null or empty");

        var keyBytes = Convert.FromBase64String(key);
        if (keyBytes.Length != 32) // 256 bits
            throw new ArgumentException("Key must be 256 bits (32 bytes)");

        var encryptedBytes = Convert.FromBase64String(encryptedData);
        if (encryptedBytes.Length < 28) // IV (12) + tag (16) = minimum 28 bytes
            throw new ArgumentException("Invalid encrypted data format");

        var iv = encryptedBytes[0..12];
        var tag = encryptedBytes[^16..];
        var ciphertext = encryptedBytes[12..^16];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(keyBytes, 16);
        aes.Decrypt(iv, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    /// <summary>
    /// Generates a secure 256-bit encryption key
    /// </summary>
    /// <returns>Base64-encoded 256-bit key</returns>
    public static string GenerateKey()
    {
        var keyBytes = new byte[32]; // 256 bits
        RandomNumberGenerator.Fill(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }

    /// <summary>
    /// Sanitizes payment data for logging by removing sensitive information
    /// </summary>
    /// <param name="data">Payment data to sanitize</param>
    /// <returns>Sanitized data safe for logging</returns>
    public static string SanitizeForLogging(string data)
    {
        if (string.IsNullOrEmpty(data))
            return string.Empty;

        // Common sensitive fields to mask
        var sensitiveFields = new[]
        {
            "card_number", "cardNumber", "pan",
            "cvv", "cvc", "security_code",
            "password", "pin",
            "secret", "key", "token",
            "signature", "hash"
        };

        var sanitized = data;
        foreach (var field in sensitiveFields)
        {
            // Mask JSON field values
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized,
                $@"""{field}"":\s*""[^""]*""",
                $@"""{field}"":""***""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            // Mask form field values
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized,
                $@"{field}=[^&]*",
                $"{field}=***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        }

        return sanitized;
    }
}