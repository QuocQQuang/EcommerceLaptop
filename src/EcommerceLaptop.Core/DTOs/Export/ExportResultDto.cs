namespace EcommerceLaptop.Core.DTOs.Export;

public class ExportResultDto
{
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}
