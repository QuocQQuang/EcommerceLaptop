using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Services;

public interface IPdfExportService
{
    /// <summary>
    /// Xuất hóa đơn PDF với chữ ký số
    /// </summary>
    /// <param name="order">Đơn hàng cần xuất hóa đơn</param>
    /// <param name="includeDigitalSignature">Có bao gồm chữ ký số hay không</param>
    /// <returns>Byte array của file PDF</returns>
    Task<byte[]> ExportInvoicePdfAsync(Order order, bool includeDigitalSignature = true);

    /// <summary>
    /// Xuất hóa đơn XML theo chuẩn Việt Nam
    /// </summary>
    /// <param name="order">Đơn hàng cần xuất hóa đơn</param>
    /// <returns>Byte array của file XML</returns>
    Task<byte[]> ExportInvoiceXmlAsync(Order order);

    /// <summary>
    /// Tạo chữ ký số cho PDF
    /// </summary>
    /// <param name="pdfBytes">Nội dung PDF cần ký</param>
    /// <param name="signatureInfo">Thông tin chữ ký</param>
    /// <returns>PDF đã được ký số</returns>
    Task<byte[]> SignPdfAsync(byte[] pdfBytes, DigitalSignatureInfo signatureInfo);
}

public class DigitalSignatureInfo
{
    public string SignerName { get; set; } = string.Empty;
    public string SignerPosition { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Reason { get; set; } = "Hóa đơn điện tử";
    public string Location { get; set; } = "Việt Nam";
    public DateTime SigningTime { get; set; } = DateTime.UtcNow;
}
