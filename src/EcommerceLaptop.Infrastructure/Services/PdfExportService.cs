using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using Microsoft.Extensions.Logging;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Signatures;
using iText.Kernel.Pdf.Xobject;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout.Borders;
using iText.IO.Font;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Math;
using System.Globalization;
using System.Text;
using System.Xml;

namespace EcommerceLaptop.Infrastructure.Services;

public class PdfExportService : IPdfExportService
{
    private readonly ILogger<PdfExportService> _logger;

    public PdfExportService(ILogger<PdfExportService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> ExportInvoicePdfAsync(Order order, bool includeDigitalSignature = true)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, PageSize.A4);

            // To font h tr Unicode (ting Vit). u tin dng TTF h thng nu c
            var (font, boldFont) = CreateUnicodeFonts();

            // Header
            var header = new Paragraph("HA N BN HNG")
                .SetFont(boldFont)
                .SetFontSize(20)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20);
            document.Add(header);

            // Thng tin cng ty
            var companyInfo = new Table(2)
                .SetWidth(UnitValue.CreatePercentValue(100))
                .SetMarginBottom(20);

            companyInfo.AddCell(new Cell()
                .Add(new Paragraph("CNG TY TNHH LAPTOP STORE")
                    .SetFont(boldFont)
                    .SetFontSize(14))
                .Add(new Paragraph("a ch: 123 ng ABC, Qun XYZ, TP.HCM")
                    .SetFont(font)
                    .SetFontSize(10))
                .Add(new Paragraph("in thoi: 0123-456-789")
                    .SetFont(font)
                    .SetFontSize(10))
                .Add(new Paragraph("Email: info@laptopstore.com")
                    .SetFont(font)
                    .SetFontSize(10))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));

            companyInfo.AddCell(new Cell()
                .Add(new Paragraph($"S ha n: {order.OrderNumber}")
                    .SetFont(boldFont)
                    .SetFontSize(12))
                .Add(new Paragraph($"Ngy: {order.OrderDate:dd/MM/yyyy HH:mm}")
                    .SetFont(font)
                    .SetFontSize(10))
                .Add(new Paragraph($"Trng thi: {GetOrderStatusText(order.Status)}")
                    .SetFont(font)
                    .SetFontSize(10))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT));

            document.Add(companyInfo);

            // Thng tin khch hng
            var customerInfo = new Paragraph("THNG TIN KHCH HNG")
                .SetFont(boldFont)
                .SetFontSize(12)
                .SetMarginTop(20)
                .SetMarginBottom(10);

            document.Add(customerInfo);

            var customerTable = new Table(2)
                .SetWidth(UnitValue.CreatePercentValue(100))
                .SetMarginBottom(20);

            customerTable.AddCell(new Cell()
                .Add(new Paragraph($"Tn: {order.User.FirstName} {order.User.LastName}")
                    .SetFont(font)
                    .SetFontSize(10))
                .Add(new Paragraph($"Email: {order.User.Email}")
                    .SetFont(font)
                    .SetFontSize(10))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));

            customerTable.AddCell(new Cell()
                .Add(new Paragraph($"a ch giao hng:")
                    .SetFont(font)
                    .SetFontSize(10))
                .Add(new Paragraph($"{order.ShippingAddress.Street}")
                    .SetFont(font)
                    .SetFontSize(10))
                .Add(new Paragraph($"{order.ShippingAddress.City}, {order.ShippingAddress.Province}")
                    .SetFont(font)
                    .SetFontSize(10))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));

            document.Add(customerTable);

            // Chi tit sn phm
            var productHeader = new Paragraph("CHI TIT SN PHM")
                .SetFont(boldFont)
                .SetFontSize(12)
                .SetMarginTop(20)
                .SetMarginBottom(10);

            document.Add(productHeader);

            var productTable = new Table(5)
                .SetWidth(UnitValue.CreatePercentValue(100))
                .SetMarginBottom(20);

            // Header row
            productTable.AddHeaderCell(new Cell()
                .Add(new Paragraph("STT").SetFont(boldFont).SetFontSize(10))
                .SetTextAlignment(TextAlignment.CENTER));
            productTable.AddHeaderCell(new Cell()
                .Add(new Paragraph("Tn sn phm").SetFont(boldFont).SetFontSize(10))
                .SetTextAlignment(TextAlignment.CENTER));
            productTable.AddHeaderCell(new Cell()
                .Add(new Paragraph("S lng").SetFont(boldFont).SetFontSize(10))
                .SetTextAlignment(TextAlignment.CENTER));
            productTable.AddHeaderCell(new Cell()
                .Add(new Paragraph("n gi").SetFont(boldFont).SetFontSize(10))
                .SetTextAlignment(TextAlignment.CENTER));
            productTable.AddHeaderCell(new Cell()
                .Add(new Paragraph("Thnh tin").SetFont(boldFont).SetFontSize(10))
                .SetTextAlignment(TextAlignment.CENTER));

            // Product rows
            int stt = 1;
            foreach (var item in order.OrderItems)
            {
                productTable.AddCell(new Cell()
                    .Add(new Paragraph(stt.ToString()).SetFont(font).SetFontSize(10))
                    .SetTextAlignment(TextAlignment.CENTER));
                productTable.AddCell(new Cell()
                    .Add(new Paragraph(item.Product.Name).SetFont(font).SetFontSize(10)));
                productTable.AddCell(new Cell()
                    .Add(new Paragraph(item.Quantity.ToString()).SetFont(font).SetFontSize(10))
                    .SetTextAlignment(TextAlignment.CENTER));
                productTable.AddCell(new Cell()
                    .Add(new Paragraph($"${item.UnitPrice:N2}").SetFont(font).SetFontSize(10))
                    .SetTextAlignment(TextAlignment.RIGHT));
                productTable.AddCell(new Cell()
                    .Add(new Paragraph($"${item.TotalPrice:N2}").SetFont(font).SetFontSize(10))
                    .SetTextAlignment(TextAlignment.RIGHT));
                stt++;
            }

            document.Add(productTable);

            // Tng tin
            var totalTable = new Table(2)
                .SetWidth(UnitValue.CreatePercentValue(50))
                .SetHorizontalAlignment(HorizontalAlignment.RIGHT)
                .SetMarginBottom(20);

            totalTable.AddCell(new Cell()
                .Add(new Paragraph("Tm tnh:").SetFont(font).SetFontSize(10))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            totalTable.AddCell(new Cell()
                .Add(new Paragraph($"${order.SubTotal:N2}").SetFont(font).SetFontSize(10))
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));

            if (order.DiscountAmount > 0)
            {
                totalTable.AddCell(new Cell()
                    .Add(new Paragraph("Gim gi:").SetFont(font).SetFontSize(10))
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                totalTable.AddCell(new Cell()
                    .Add(new Paragraph($"-${order.DiscountAmount:N2}").SetFont(font).SetFontSize(10))
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            }

            totalTable.AddCell(new Cell()
                .Add(new Paragraph("Ph vn chuyn:").SetFont(font).SetFontSize(10))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            totalTable.AddCell(new Cell()
                .Add(new Paragraph($"${order.ShippingAmount:N2}").SetFont(font).SetFontSize(10))
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));

            totalTable.AddCell(new Cell()
                .Add(new Paragraph("Thu:").SetFont(font).SetFontSize(10))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            totalTable.AddCell(new Cell()
                .Add(new Paragraph($"${order.TaxAmount:N2}").SetFont(font).SetFontSize(10))
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));

            totalTable.AddCell(new Cell()
                .Add(new Paragraph("TNG CNG:").SetFont(boldFont).SetFontSize(12))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            totalTable.AddCell(new Cell()
                .Add(new Paragraph($"${order.TotalAmount:N2}").SetFont(boldFont).SetFontSize(12))
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));

            document.Add(totalTable);

            // Ch k
            var signatureTable = new Table(2)
                .SetWidth(UnitValue.CreatePercentValue(100))
                .SetMarginTop(40);

            signatureTable.AddCell(new Cell()
                .Add(new Paragraph("Ngi mua").SetFont(font).SetFontSize(10))
                .Add(new Paragraph("(K tn)").SetFont(font).SetFontSize(8))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetHeight(60));

            signatureTable.AddCell(new Cell()
                .Add(new Paragraph("Ngi bn").SetFont(font).SetFontSize(10))
                .Add(new Paragraph("(K tn)").SetFont(font).SetFontSize(8))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetHeight(60));

            document.Add(signatureTable);

            document.Close();

            var pdfBytes = memoryStream.ToArray();

            // Thm ch k s nu c yu cu
            if (includeDigitalSignature)
            {
                var signatureInfo = new DigitalSignatureInfo
                {
                    SignerName = "Laptop Store Admin",
                    SignerPosition = "Gim c",
                    CompanyName = "CNG TY TNHH LAPTOP STORE",
                    Reason = "Ha n in t",
                    Location = "TP.HCM, Vit Nam",
                    SigningTime = DateTime.UtcNow
                };

                pdfBytes = await SignPdfAsync(pdfBytes, signatureInfo);
            }

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF invoice for order {OrderId}", order.Id);
            throw;
        }
    }

    public Task<byte[]> ExportInvoiceXmlAsync(Order order)
    {
        try
        {
            using var stream = new MemoryStream();
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                Indent = true,
                Async = false
            };

            using (var writer = XmlWriter.Create(stream, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Invoice");

                writer.WriteElementString("InvoiceNumber", order.OrderNumber);
                writer.WriteElementString("InvoiceDate", order.OrderDate.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
                writer.WriteElementString("Status", order.Status.ToString());

                writer.WriteStartElement("Company");
                writer.WriteElementString("Name", "Công ty TNHH Laptop Store");
                writer.WriteElementString("Address", "123 đường ABC, Quận XYZ, TP.HCM");
                writer.WriteElementString("Phone", "0123-456-789");
                writer.WriteElementString("Email", "info@laptopstore.com");
                writer.WriteEndElement();

                writer.WriteStartElement("Customer");
                writer.WriteElementString("Name", $"{order.User.FirstName} {order.User.LastName}".Trim());
                writer.WriteElementString("Email", order.User.Email);
                writer.WriteElementString("Address", FormatAddress(order.ShippingAddress));
                writer.WriteEndElement();

                writer.WriteStartElement("Items");
            foreach (var item in order.OrderItems)
            {
                    writer.WriteStartElement("Item");
                    writer.WriteElementString("ProductName", item.Product?.Name ?? string.Empty);
                    writer.WriteElementString("Quantity", item.Quantity.ToString(CultureInfo.InvariantCulture));
                    writer.WriteElementString("UnitPrice", FormatMoney(item.UnitPrice));
                    writer.WriteElementString("TotalPrice", FormatMoney(item.TotalPrice));
                    writer.WriteEndElement();
            }
                writer.WriteEndElement();

                writer.WriteStartElement("Totals");
                writer.WriteElementString("SubTotal", FormatMoney(order.SubTotal));
                writer.WriteElementString("DiscountAmount", FormatMoney(order.DiscountAmount));
                writer.WriteElementString("ShippingAmount", FormatMoney(order.ShippingAmount));
                writer.WriteElementString("TaxAmount", FormatMoney(order.TaxAmount));
                writer.WriteElementString("TotalAmount", FormatMoney(order.TotalAmount));
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            return Task.FromResult(stream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating XML invoice for order {OrderId}", order.Id);
            throw;
        }

        static string FormatMoney(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        static string FormatAddress(EcommerceLaptop.Core.ValueObjects.Address? address)
        {
            if (address is null)
            {
                return string.Empty;
            }

            return string.Join(", ", new[]
            {
                address.Street,
                address.City,
                address.Province,
                address.PostalCode,
                address.Country
            }.Where(part => !string.IsNullOrWhiteSpace(part)));
        }
    }

    public async Task<byte[]> SignPdfAsync(byte[] pdfBytes, DigitalSignatureInfo signatureInfo)
    {
        try
        {
            // n gin ha: ch thm thng tin ch k vo PDF m khng thc s k s
            // Trong thc t, cn c chng ch s hp l t CA
            using var inputStream = new MemoryStream(pdfBytes);
            using var outputStream = new MemoryStream();

            using var reader = new PdfReader(inputStream);
            using var writer = new PdfWriter(outputStream);
            using var pdfDoc = new PdfDocument(reader, writer);

            // Thm thng tin ch k vo metadata
            var info = pdfDoc.GetDocumentInfo();
            info.SetTitle($"Ha n - {signatureInfo.CompanyName}");
            info.SetAuthor(signatureInfo.SignerName);
            info.SetSubject(signatureInfo.Reason);
            info.SetCreator("EcommerceLaptop System");
            info.SetKeywords($"Invoice, {signatureInfo.CompanyName}, {signatureInfo.SigningTime:yyyy-MM-dd}");

            // Thm thng tin ch k vo trang cui
            var lastPage = pdfDoc.GetLastPage();
            var canvas = new PdfCanvas(lastPage);
            var (font, _) = CreateUnicodeFonts();

            canvas.BeginText()
                .SetFontAndSize(font, 8)
                .MoveText(50, 50)
                .ShowText($"K bi: {signatureInfo.SignerName} - {signatureInfo.SignerPosition}")
                .MoveText(0, -10)
                .ShowText($"Cng ty: {signatureInfo.CompanyName}")
                .MoveText(0, -10)
                .ShowText($"Ngy k: {signatureInfo.SigningTime:dd/MM/yyyy HH:mm}")
                .MoveText(0, -10)
                .ShowText($"L do: {signatureInfo.Reason}")
                .EndText();

            pdfDoc.Close();

            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing PDF");
            // Tr v PDF gc nu c li
            return pdfBytes;
        }
    }


    private (PdfFont Normal, PdfFont Bold) CreateUnicodeFonts()
    {
        try
        {
            var fontCandidates = new (string normal, string bold)[]
            {
                ("C:\\Windows\\Fonts\\arialuni.ttf", "C:\\Windows\\Fonts\\arialbd.ttf"), // Arial Unicode MS + Arial Bold
                ("C:\\Windows\\Fonts\\segoeui.ttf", "C:\\Windows\\Fonts\\segoeuib.ttf"), // Segoe UI
                ("C:\\Windows\\Fonts\\arial.ttf", "C:\\Windows\\Fonts\\arialbd.ttf"),     // Arial
                ("C:\\Windows\\Fonts\\tahoma.ttf", "C:\\Windows\\Fonts\\tahomabd.ttf"), // Tahoma
            };

            foreach (var candidate in fontCandidates)
            {
                if (File.Exists(candidate.normal))
                {
                    var normalProgram = FontProgramFactory.CreateFont(candidate.normal);
                    var normal = PdfFontFactory.CreateFont(normalProgram, PdfEncodings.IDENTITY_H);
                    PdfFont bold;
                    if (File.Exists(candidate.bold))
                    {
                        var boldProgram = FontProgramFactory.CreateFont(candidate.bold);
                        bold = PdfFontFactory.CreateFont(boldProgram, PdfEncodings.IDENTITY_H);
                    }
                    else
                    {
                        bold = normal;
                    }
                    return (normal, bold);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falling back to standard fonts for PDF due to Unicode font load failure");
        }

        // Fallback to standard fonts without Identity-H to avoid encoding errors
        var fallbackNormal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        var fallbackBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        return (fallbackNormal, fallbackBold);
    }

    private string GetOrderStatusText(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Pending => "Ch x l",
            OrderStatus.Confirmed => " xc nhn",
            OrderStatus.Processing => "ang x l",
            OrderStatus.Shipped => " giao hng",
            OrderStatus.Delivered => " nhn hng",
            OrderStatus.Cancelled => " hy",
            OrderStatus.Returned => " tr hng",
            _ => "Khng xc nh"
        };
    }
}
