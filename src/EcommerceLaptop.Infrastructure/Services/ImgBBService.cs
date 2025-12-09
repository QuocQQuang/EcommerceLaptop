using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EcommerceLaptop.Core.Configuration;
using EcommerceLaptop.Core.Services;

namespace EcommerceLaptop.Infrastructure.Services;

/// <summary>
/// ImgBB API service implementation using simple API key authentication
/// Provides reliable image hosting with straightforward integration
/// </summary>
public class ImgBBService : IImageHostingService
{
    private readonly HttpClient _httpClient;
    private readonly ImgBBSettings _settings;
    private readonly ILogger<ImgBBService> _logger;

    public ImgBBService(
        HttpClient httpClient,
        IOptions<ImgBBSettings> settings,
        ILogger<ImgBBService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        // Configure HttpClient
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<ImageUploadResult> UploadImageAsync(Stream imageStream, string fileName, ImageCategory category = ImageCategory.General)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            throw new InvalidOperationException("ImgBB API key is not configured");
        }

        if (!IsValidImage(fileName, GetContentType(fileName), imageStream.Length))
        {
            throw new ImageHostingException("Invalid image file", 400, "INVALID_FILE");
        }

        try
        {
            // Reset stream position if needed
            if (imageStream.CanSeek)
                imageStream.Position = 0;

            // Prepare multipart content
            using var content = new MultipartFormDataContent();

            // Add API key
            content.Add(new StringContent(_settings.ApiKey), "key");

            // Add image with category prefix for organization
            var prefixedName = GetCategoryPrefix(category) + fileName;
            content.Add(new StreamContent(imageStream), "image", prefixedName);

            // Add optional expiration
            if (_settings.ExpirationSeconds > 0)
            {
                content.Add(new StringContent(_settings.ExpirationSeconds.ToString()), "expiration");
            }

            // Upload with retry logic
            var response = await UploadWithRetryAsync(content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("ImgBB upload failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new ImageHostingException($"Upload failed: {response.StatusCode}", (int)response.StatusCode, "UPLOAD_FAILED");
            }

            // Parse response
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("ImgBB API Response: {Response}", responseContent);
            var imgbbResponse = JsonSerializer.Deserialize<ImgBBApiResponse>(responseContent);

            if (imgbbResponse?.Data == null || !imgbbResponse.Success)
            {
                _logger.LogError("ImgBB Response Deserialization Failed. Success: {Success}, Data: {Data}",
                    imgbbResponse?.Success, imgbbResponse?.Data != null ? "Not Null" : "Null");
                throw new ImageHostingException("Invalid response from ImgBB API", 500, "INVALID_RESPONSE");
            }

            _logger.LogInformation("Successfully uploaded image to ImgBB: {ImageId}", imgbbResponse.Data.Id);

            return new ImageUploadResult
            {
                Url = imgbbResponse.Data.DisplayUrl,
                DeleteUrl = imgbbResponse.Data.DeleteUrl,
                ImageId = imgbbResponse.Data.Id,
                Width = imgbbResponse.Data.Width,
                Height = imgbbResponse.Data.Height,
                Size = imgbbResponse.Data.Size,
                FileName = prefixedName
            };
        }
        catch (ImageHostingException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during image upload");
            throw new ImageHostingException("Upload failed due to unexpected error", 500, "INTERNAL_ERROR");
        }
    }

    public async Task<bool> DeleteImageAsync(string deleteUrl)
    {
        if (string.IsNullOrEmpty(deleteUrl))
        {
            _logger.LogWarning("Attempted to delete image with empty delete URL");
            return false;
        }

        try
        {
            // ImgBB uses delete URL for deletion (simple GET request)
            var response = await _httpClient.GetAsync(deleteUrl);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully deleted image: {DeleteUrl}", deleteUrl);
                return true;
            }

            _logger.LogWarning("Failed to delete image {DeleteUrl}: {StatusCode}", deleteUrl, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {DeleteUrl}", deleteUrl);
            return false;
        }
    }

    public bool IsValidImage(string fileName, string contentType, long fileSize)
    {
        // Check file size (ImgBB supports up to 32MB)
        if (fileSize > _settings.MaxFileSizeBytes)
        {
            _logger.LogWarning("File size {FileSize} exceeds limit {MaxSize}", fileSize, _settings.MaxFileSizeBytes);
            return false;
        }

        if (fileSize <= 0)
        {
            _logger.LogWarning("File size is zero or negative: {FileSize}", fileSize);
            return false;
        }

        // Check MIME type
        if (!_settings.AllowedMimeTypes.Contains(contentType?.ToLowerInvariant()))
        {
            _logger.LogWarning("Invalid content type: {ContentType}", contentType);
            return false;
        }

        // Check file extension
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };

        if (!allowedExtensions.Contains(extension))
        {
            _logger.LogWarning("Invalid file extension: {Extension}", extension);
            return false;
        }

        return true;
    }

    private async Task<HttpResponseMessage> UploadWithRetryAsync(MultipartFormDataContent content)
    {
        HttpResponseMessage? response = null;
        Exception? lastException = null;

        for (int attempt = 1; attempt <= _settings.MaxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Starting upload attempt {Attempt}/{MaxRetries} to ImgBB",
                    attempt, _settings.MaxRetries);

                response = await _httpClient.PostAsync(_settings.ApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Upload attempt {Attempt} succeeded", attempt);
                    return response;
                }

                if (attempt < _settings.MaxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                    _logger.LogWarning("Upload attempt {Attempt} failed with {StatusCode}, retrying in {Delay}s",
                        attempt, response.StatusCode, delay.TotalSeconds);
                    await Task.Delay(delay);
                }
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt < _settings.MaxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    _logger.LogWarning(ex, "Upload attempt {Attempt} failed, retrying in {Delay}s",
                        attempt, delay.TotalSeconds);
                    await Task.Delay(delay);
                }
            }
        }

        if (response != null)
        {
            return response;
        }

        throw lastException ?? new ImageHostingException("All retry attempts failed", 500, "RETRY_EXHAUSTED");
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "application/octet-stream"
        };
    }

    private static string GetCategoryPrefix(ImageCategory category)
    {
        return category switch
        {
            ImageCategory.Avatars => "avatar_",
            ImageCategory.Products => "product_",
            ImageCategory.Reviews => "review_",
            ImageCategory.Blogs => "blog_",
            _ => "general_"
        };
    }
}

/// <summary>
/// ImgBB API response structure
/// </summary>
internal class ImgBBApiResponse
{
    [JsonPropertyName("data")]
    public ImgBBImageData? Data { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }
}

/// <summary>
/// ImgBB image data structure
/// </summary>
internal class ImgBBImageData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("display_url")]
    public string DisplayUrl { get; set; } = string.Empty;

    [JsonPropertyName("delete_url")]
    public string DeleteUrl { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }
}

/// <summary>
/// Custom exception for image hosting operations
/// </summary>
public class ImageHostingException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }

    public ImageHostingException(string message, int statusCode = 500, string errorCode = "UNKNOWN")
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public ImageHostingException(string message, Exception innerException, int statusCode = 500, string errorCode = "UNKNOWN")
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}