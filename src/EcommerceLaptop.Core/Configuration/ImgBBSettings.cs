namespace EcommerceLaptop.Core.Configuration;

/// <summary>
/// Configuration settings for ImgBB API integration
/// Simple API key-based authentication for image hosting
/// </summary>
public class ImgBBSettings
{
    /// <summary>
    /// ImgBB API key for authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// ImgBB API upload endpoint
    /// </summary>
    public string ApiUrl { get; set; } = "https://api.imgbb.com/1/upload";

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum retry attempts for failed requests
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Maximum file size allowed (bytes) - ImgBB limit is 32MB
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB default

    /// <summary>
    /// Allowed MIME types for image uploads
    /// </summary>
    public string[] AllowedMimeTypes { get; set; } =
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/bmp"
    };

    /// <summary>
    /// Image expiration time in seconds (optional, 0 = never expires)
    /// </summary>
    public int ExpirationSeconds { get; set; } = 0; // Never expires by default
}

/// <summary>
/// Image categories for organization (ImgBB doesn't support albums, used for naming convention)
/// </summary>
// ImageCategory enum is defined in EcommerceLaptop.Core.Services to avoid duplicate definitions