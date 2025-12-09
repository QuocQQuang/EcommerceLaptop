using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Services;

public interface IPdfExportService
{
    /// <summary>
    /// Xut ha n PDF vi ch k s
    /// </summary>
    /// <param name="order">n hng cn xut ha n</param>
    /// <param name="includeDigitalSignature">C bao gm ch k s hay khng</param>
    /// <returns>Byte array ca file PDF</returns>
    Task<byte[]> ExportInvoicePdfAsync(Order order, bool includeDigitalSignature = true);

    /// <summary>
    /// Xut ha n XML theo chun Vit Nam
    /// </summary>
    /// <param name="order">n hng cn xut ha n</param>
    /// <returns>Byte array ca file XML</returns>
    Task<byte[]> ExportInvoiceXmlAsync(Order order);

    /// <summary>
    /// To ch k s cho PDF
    /// </summary>
    /// <param name="pdfBytes">Ni dung PDF cn k</param>
    /// <param name="signatureInfo">Thng tin ch k</param>
    /// <returns>PDF  c k s</returns>
    Task<byte[]> SignPdfAsync(byte[] pdfBytes, DigitalSignatureInfo signatureInfo);
}

public class DigitalSignatureInfo
{
    public string SignerName { get; set; } = string.Empty;
    public string SignerPosition { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Reason { get; set; } = "Ha n in t";
    public string Location { get; set; } = "Vit Nam";
    public DateTime SigningTime { get; set; } = DateTime.UtcNow;
}
