using System.Security.Cryptography;
using System.Text;

namespace EcommerceLaptop.Infrastructure.Services.Security;

/// <summary>
/// Lightweight field encryption utility for EF value conversions.
/// Initialize once with a stable secret key from configuration.
/// </summary>
public static class FieldEncryption
{
    private static byte[] _key = Array.Empty<byte>();
    private static bool _initialized = false;

    public static void Initialize(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            // Development fallback key (do NOT use in production)
            secret = "DEV_FIELD_ENCRYPTION_KEY_32_CHARS_MIN_LEN";
        }
        // Normalize to 32 bytes (AES-256)
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        if (keyBytes.Length < 32)
        {
            var padded = new byte[32];
            Array.Copy(keyBytes, padded, keyBytes.Length);
            _key = padded;
        }
        else if (keyBytes.Length > 32)
        {
            _key = keyBytes.Take(32).ToArray();
        }
        else
        {
            _key = keyBytes;
        }
        _initialized = true;
    }

    public static string Encrypt(string? plaintext)
    {
        if (!_initialized || string.IsNullOrEmpty(plaintext)) return plaintext ?? string.Empty;
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        // Store IV + cipher in base64
        var combined = new byte[aes.IV.Length + cipherBytes.Length];
        Array.Copy(aes.IV, 0, combined, 0, aes.IV.Length);
        Array.Copy(cipherBytes, 0, combined, aes.IV.Length, cipherBytes.Length);
        return Convert.ToBase64String(combined);
    }

    public static string Decrypt(string? ciphertext)
    {
        if (!_initialized || string.IsNullOrEmpty(ciphertext)) return ciphertext ?? string.Empty;
        try
        {
            var combined = Convert.FromBase64String(ciphertext);
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            var iv = new byte[aes.BlockSize / 8];
            Array.Copy(combined, 0, iv, 0, iv.Length);
            aes.IV = iv;
            var cipherBytes = new byte[combined.Length - iv.Length];
            Array.Copy(combined, iv.Length, cipherBytes, 0, cipherBytes.Length);
            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            // If value isn't encrypted or corrupted, return original to avoid data loss
            return ciphertext!;
        }
    }
}


