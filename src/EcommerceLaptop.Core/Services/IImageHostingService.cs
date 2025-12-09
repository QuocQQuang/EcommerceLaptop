namespace EcommerceLaptop.Core.Services;

/// <summary>
/// Service interface for image hosting service integration
/// Handles image uploads and management for the application
/// </summary>
public interface IImageHostingService
{
    /// <summary>
    /// Uploads an image to the hosting service
    /// </summary>
    /// <param name="imageStream">Image file stream</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="category">Image category for organization</param>
    /// <returns>Upload result with hosting service URL and metadata</returns>
    Task<ImageUploadResult> UploadImageAsync(Stream imageStream, string fileName, ImageCategory category = ImageCategory.General);

    /// <summary>
    /// Deletes an image from the hosting service
    /// </summary>
    /// <param name="deleteUrl">Delete URL returned from upload</param>
    /// <returns>True if successfully deleted</returns>
    Task<bool> DeleteImageAsync(string deleteUrl);

    /// <summary>
    /// Validates if the file is a supported image format
    /// </summary>
    /// <param name="fileName">File name with extension</param>
    /// <param name="contentType">MIME content type</param>
    /// <param name="fileSize">File size in bytes</param>
    /// <returns>True if valid image</returns>
    bool IsValidImage(string fileName, string contentType, long fileSize);
}

/// <summary>
/// Image categories for organization
/// </summary>
public enum ImageCategory
{
    General,
    Avatars,
    Products,
    Reviews,
    Blogs
}

/// <summary>
/// Result of image upload operation
/// </summary>
public class ImageUploadResult
{
    public string Url { get; set; } = string.Empty;
    public string DeleteUrl { get; set; } = string.Empty;
    public string ImageId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public long Size { get; set; }
}