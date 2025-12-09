using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using Microsoft.Extensions.Logging;
using ClosedXML.Excel;
using System.Globalization;

namespace EcommerceLaptop.Infrastructure.Services;

public class ExcelExportService : IExcelExportService
{
    private readonly ILogger<ExcelExportService> _logger;

    public ExcelExportService(ILogger<ExcelExportService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> ExportOrdersToExcelAsync(IEnumerable<Order> orders, string fileName = "orders")
    {
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("n hng");

            // Header
            var headers = new[]
            {
                "STT", "M n hng", "Khch hng", "Email", "Ngy t", "Trng thi",
                "Tm tnh", "Gim gi", "Ph vn chuyn", "Thu", "Tng tin", "a ch giao hng"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            // Data
            int row = 2;
            int stt = 1;
            foreach (var order in orders)
            {
                worksheet.Cell(row, 1).Value = stt;
                worksheet.Cell(row, 2).Value = order.OrderNumber;
                worksheet.Cell(row, 3).Value = $"{order.User.FirstName} {order.User.LastName}";
                worksheet.Cell(row, 4).Value = order.User.Email;
                worksheet.Cell(row, 5).Value = order.OrderDate.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell(row, 6).Value = GetOrderStatusText(order.Status);
                worksheet.Cell(row, 7).Value = order.SubTotal;
                worksheet.Cell(row, 8).Value = order.DiscountAmount;
                worksheet.Cell(row, 9).Value = order.ShippingAmount;
                worksheet.Cell(row, 10).Value = order.TaxAmount;
                worksheet.Cell(row, 11).Value = order.TotalAmount;
                worksheet.Cell(row, 12).Value = $"{order.ShippingStreet}, {order.ShippingCity}, {order.ShippingProvince}";

                // Format currency columns as USD
                worksheet.Cell(row, 7).Style.NumberFormat.Format = "$#,##0.00";
                worksheet.Cell(row, 8).Style.NumberFormat.Format = "$#,##0.00";
                worksheet.Cell(row, 9).Style.NumberFormat.Format = "$#,##0.00";
                worksheet.Cell(row, 10).Style.NumberFormat.Format = "$#,##0.00";
                worksheet.Cell(row, 11).Style.NumberFormat.Format = "$#,##0.00";

                row++;
                stt++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add borders
            worksheet.Range(1, 1, row - 1, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(1, 1, row - 1, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting orders to Excel");
            throw;
        }
    }

    public async Task<byte[]> ExportProductsToExcelAsync(IEnumerable<Product> products, string fileName = "products")
    {
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sn phm");

            // Header
            var headers = new[]
            {
                "STT", "Tn sn phm", "M t", "Gi", "Gi khuyn mi", "S lng tn kho",
                "Thng hiu", "Danh mc", "Trng thi", "Ngy to", "Ngy cp nht"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
            }

            // Data
            int row = 2;
            int stt = 1;
            foreach (var product in products)
            {
                worksheet.Cell(row, 1).Value = stt;
                worksheet.Cell(row, 2).Value = product.Name;
                worksheet.Cell(row, 3).Value = product.Description?.Length > 100 ? product.Description.Substring(0, 100) + "..." : product.Description;
                worksheet.Cell(row, 4).Value = product.Price;
                worksheet.Cell(row, 5).Value = product.Price; // Using Price instead of SalePrice
                worksheet.Cell(row, 6).Value = product.Inventory?.QuantityInStock ?? 0; // Using Inventory.QuantityInStock instead of StockQuantity
                worksheet.Cell(row, 7).Value = product.ProductBrand?.Name ?? product.Brand ?? "N/A";
                worksheet.Cell(row, 8).Value = product.Category?.Name ?? "N/A";
                worksheet.Cell(row, 9).Value = product.IsActive ? "Hot ng" : "Khng hot ng";
                worksheet.Cell(row, 10).Value = product.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell(row, 11).Value = product.UpdatedAt.ToString("dd/MM/yyyy HH:mm");

                // Format currency columns as USD
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
                worksheet.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";

                row++;
                stt++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add borders
            worksheet.Range(1, 1, row - 1, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(1, 1, row - 1, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting products to Excel");
            throw;
        }
    }

    public async Task<byte[]> ExportRevenueReportToExcelAsync(DateTime startDate, DateTime endDate, string fileName = "revenue_report")
    {
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Bo co doanh thu");

            // Title
            worksheet.Cell(1, 1).Value = $"BO CO DOANH THU T {startDate:dd/MM/yyyy} N {endDate:dd/MM/yyyy}";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range(1, 1, 1, 8).Merge();

            // Header
            var headers = new[]
            {
                "Ngy", "S n hng", "Tng doanh thu", "Doanh thu trung bnh/n",
                "S sn phm bn", "Khch hng mi", "Khch hng c", "Ghi ch"
            };

            int headerRow = 3;
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(headerRow, i + 1).Value = headers[i];
                worksheet.Cell(headerRow, i + 1).Style.Font.Bold = true;
                worksheet.Cell(headerRow, i + 1).Style.Fill.BackgroundColor = XLColor.LightYellow;
            }

            // Sample data (in real implementation, this would come from database)
            var currentDate = startDate;
            int dataRow = 4;
            Random random = new Random();

            while (currentDate <= endDate)
            {
                var orderCount = random.Next(1, 5);
                var totalRevenue = random.Next(10000000, 50000000);
                var avgRevenue = totalRevenue / orderCount;
                var productsSold = random.Next(10, 50);
                var newCustomers = random.Next(2, 8);
                var returningCustomers = orderCount - newCustomers;

                worksheet.Cell(dataRow, 1).Value = currentDate.ToString("dd/MM/yyyy");
                worksheet.Cell(dataRow, 2).Value = orderCount;
                worksheet.Cell(dataRow, 3).Value = totalRevenue;
                worksheet.Cell(dataRow, 4).Value = avgRevenue;
                worksheet.Cell(dataRow, 5).Value = productsSold;
                worksheet.Cell(dataRow, 6).Value = newCustomers;
                worksheet.Cell(dataRow, 7).Value = returningCustomers;
                worksheet.Cell(dataRow, 8).Value = "";

                // Format currency columns as VND
                worksheet.Cell(dataRow, 3).Style.NumberFormat.Format = "[$-vi-VN] #,####0";
                worksheet.Cell(dataRow, 4).Style.NumberFormat.Format = "[$-vi-VN] #,####0";

                currentDate = currentDate.AddDays(1);
                dataRow++;
            }

            // Summary row
            var summaryRow = dataRow + 1;
            worksheet.Cell(summaryRow, 1).Value = "TNG CNG";
            worksheet.Cell(summaryRow, 1).Style.Font.Bold = true;
            worksheet.Cell(summaryRow, 2).FormulaA1 = $"SUM(B{headerRow + 1}:B{dataRow - 1})";
            worksheet.Cell(summaryRow, 3).FormulaA1 = $"SUM(C{headerRow + 1}:C{dataRow - 1})";
            worksheet.Cell(summaryRow, 4).FormulaA1 = $"AVERAGE(D{headerRow + 1}:D{dataRow - 1})";
            worksheet.Cell(summaryRow, 5).FormulaA1 = $"SUM(E{headerRow + 1}:E{dataRow - 1})";
            worksheet.Cell(summaryRow, 6).FormulaA1 = $"SUM(F{headerRow + 1}:F{dataRow - 1})";
            worksheet.Cell(summaryRow, 7).FormulaA1 = $"SUM(G{headerRow + 1}:G{dataRow - 1})";

            // Format summary row
            for (int i = 1; i <= 7; i++)
            {
                worksheet.Cell(summaryRow, i).Style.Font.Bold = true;
                worksheet.Cell(summaryRow, i).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add borders
            worksheet.Range(headerRow, 1, dataRow, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(headerRow, 1, dataRow, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting revenue report to Excel");
            throw;
        }
    }

    public async Task<byte[]> ExportInventoryReportToExcelAsync(string fileName = "inventory_report")
    {
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Bo co tn kho");

            // Title
            worksheet.Cell(1, 1).Value = "BO CO TN KHO";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range(1, 1, 1, 8).Merge();

            // Header
            var headers = new[]
            {
                "STT", "Tn sn phm", "M SKU", "S lng tn kho", "Gi nhp", "Gi bn",
                "Gi tr tn kho", "Trng thi", "Cnh bo"
            };

            int headerRow = 3;
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(headerRow, i + 1).Value = headers[i];
                worksheet.Cell(headerRow, i + 1).Style.Font.Bold = true;
                worksheet.Cell(headerRow, i + 1).Style.Fill.BackgroundColor = XLColor.LightCoral;
            }

            // Sample data (in real implementation, this would come from database)
            Random random = new Random();
            int dataRow = 4;
            int stt = 1;

            for (int i = 0; i < 50; i++) // Sample 50 products
            {
                var stockQuantity = random.Next(0, 100);
                var costPrice = random.Next(5000000, 50000000);
                var sellingPrice = costPrice + random.Next(1000000, 10000000);
                var inventoryValue = stockQuantity * costPrice;

                string warning = "";
                if (stockQuantity == 0)
                    warning = "HT HNG";
                else if (stockQuantity < 10)
                    warning = "SP HT HNG";
                else if (stockQuantity > 80)
                    warning = "TN KHO CAO";

                worksheet.Cell(dataRow, 1).Value = stt;
                worksheet.Cell(dataRow, 2).Value = $"Laptop {i + 1}";
                worksheet.Cell(dataRow, 3).Value = $"SKU{i + 1:D4}";
                worksheet.Cell(dataRow, 4).Value = stockQuantity;
                worksheet.Cell(dataRow, 5).Value = costPrice;
                worksheet.Cell(dataRow, 6).Value = sellingPrice;
                worksheet.Cell(dataRow, 7).Value = inventoryValue;
                worksheet.Cell(dataRow, 8).Value = stockQuantity > 0 ? "Cn hng" : "Ht hng";
                worksheet.Cell(dataRow, 9).Value = warning;

                // Format currency columns as USD
                worksheet.Cell(dataRow, 5).Style.NumberFormat.Format = "$#,##0.00";
                worksheet.Cell(dataRow, 6).Style.NumberFormat.Format = "$#,##0.00";
                worksheet.Cell(dataRow, 7).Style.NumberFormat.Format = "$#,##0.00";

                // Color coding for warnings
                if (warning == "HT HNG")
                    worksheet.Cell(dataRow, 9).Style.Font.FontColor = XLColor.Red;
                else if (warning == "SP HT HNG")
                    worksheet.Cell(dataRow, 9).Style.Font.FontColor = XLColor.Orange;
                else if (warning == "TN KHO CAO")
                    worksheet.Cell(dataRow, 9).Style.Font.FontColor = XLColor.Blue;

                dataRow++;
                stt++;
            }

            // Summary
            var summaryRow = dataRow + 1;
            worksheet.Cell(summaryRow, 1).Value = "TNG CNG";
            worksheet.Cell(summaryRow, 1).Style.Font.Bold = true;
            worksheet.Cell(summaryRow, 4).FormulaA1 = $"SUM(D{headerRow + 1}:D{dataRow - 1})";
            worksheet.Cell(summaryRow, 7).FormulaA1 = $"SUM(G{headerRow + 1}:G{dataRow - 1})";

            // Format summary row
            for (int i = 1; i <= 7; i++)
            {
                worksheet.Cell(summaryRow, i).Style.Font.Bold = true;
                worksheet.Cell(summaryRow, i).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add borders
            worksheet.Range(headerRow, 1, dataRow, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(headerRow, 1, dataRow, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting inventory report to Excel");
            throw;
        }
    }

    public async Task<byte[]> ExportUsersToExcelAsync(IEnumerable<User> users, string fileName = "users")
    {
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Khch hng");

            // Header
            var headers = new[]
            {
                "STT", "H tn", "Email", "S in thoi", "a ch", "Ngy ng k",
                "Trng thi", "VIP Level", "Tng n hng", "Tng chi tiu"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            // Data
            int row = 2;
            int stt = 1;
            foreach (var user in users)
            {
                worksheet.Cell(row, 1).Value = stt;
                worksheet.Cell(row, 2).Value = $"{user.FirstName} {user.LastName}";
                worksheet.Cell(row, 3).Value = user.Email;
                worksheet.Cell(row, 4).Value = user.PhoneNumber ?? "N/A";
                worksheet.Cell(row, 5).Value = "N/A"; // User doesn't have Address property
                worksheet.Cell(row, 6).Value = user.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell(row, 7).Value = user.IsActive ? "Hot ng" : "Khng hot ng";
                worksheet.Cell(row, 8).Value = user.VipTierId?.ToString() ?? "0";
                worksheet.Cell(row, 9).Value = user.Orders?.Count ?? 0;
                worksheet.Cell(row, 10).Value = user.Orders?.Sum(o => o.TotalAmount) ?? 0;

                // Format currency column as USD
                worksheet.Cell(row, 10).Style.NumberFormat.Format = "$#,##0.00";

                row++;
                stt++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add borders
            worksheet.Range(1, 1, row - 1, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(1, 1, row - 1, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting users to Excel");
            throw;
        }
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

    public async Task<byte[]> ExportSecurityEventsToExcelAsync(IEnumerable<SecurityEvent> securityEvents, string fileName = "security_events")
    {
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("S kin bo mt");

            // Header
            var headers = new[]
            {
                "STT", "Thi gian", "Loi s kin", "IP Address", "M t",
                "Mc ", "Trng thi", "Correlation ID", "User Agent"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightPink;
            }

            // Data
            int row = 2;
            int stt = 1;
            foreach (var evt in securityEvents)
            {
                worksheet.Cell(row, 1).Value = stt;
                worksheet.Cell(row, 2).Value = evt.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss");
                worksheet.Cell(row, 3).Value = evt.EventType;
                worksheet.Cell(row, 4).Value = evt.IPAddress ?? "N/A";
                worksheet.Cell(row, 5).Value = evt.Description;
                worksheet.Cell(row, 6).Value = evt.Severity;
                worksheet.Cell(row, 7).Value = evt.WasBlocked ? " chn" : "Cho php";
                worksheet.Cell(row, 8).Value = evt.CorrelationId ?? "N/A";
                worksheet.Cell(row, 9).Value = evt.UserAgent ?? "N/A";

                // Color code by severity
                var severityColor = evt.Severity.ToLower() switch
                {
                    "critical" => XLColor.Red,
                    "high" => XLColor.Orange,
                    "medium" => XLColor.Yellow,
                    "low" => XLColor.LightGreen,
                    _ => XLColor.White
                };
                worksheet.Cell(row, 6).Style.Fill.BackgroundColor = severityColor;

                row++;
                stt++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add borders
            worksheet.Range(1, 1, row - 1, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(1, 1, row - 1, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting security events to Excel");
            throw;
        }
    }

    public async Task<byte[]> ExportIPBlockRulesToExcelAsync(IEnumerable<IPBlockRule> ipBlockRules, string fileName = "ip_block_rules")
    {
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("IP Block Rules");

            // Header
            var headers = new[]
            {
                "STT", "IP Address", "Loi", "L do", "Trng thi",
                "Ngy to", "Ngy ht hn", "Threat Level", "Country Code"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            // Data
            int row = 2;
            int stt = 1;
            foreach (var rule in ipBlockRules)
            {
                worksheet.Cell(row, 1).Value = stt;
                worksheet.Cell(row, 2).Value = rule.IPAddress;
                worksheet.Cell(row, 3).Value = rule.Type == "blacklist" ? "Chn" : "Cho php";
                worksheet.Cell(row, 4).Value = rule.Reason;
                worksheet.Cell(row, 5).Value = rule.IsActive ? "Hot ng" : "Tm dng";
                worksheet.Cell(row, 6).Value = rule.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell(row, 7).Value = rule.ExpiresAt?.ToString("dd/MM/yyyy HH:mm") ?? "Vnh vin";
                worksheet.Cell(row, 8).Value = rule.ThreatLevel;
                worksheet.Cell(row, 9).Value = rule.CountryCode ?? "N/A";

                // Color code by type
                var typeColor = rule.Type == "blacklist" ? XLColor.LightPink : XLColor.LightGreen;
                worksheet.Cell(row, 3).Style.Fill.BackgroundColor = typeColor;

                row++;
                stt++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add borders
            worksheet.Range(1, 1, row - 1, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(1, 1, row - 1, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting IP block rules to Excel");
            throw;
        }
    }

    public async Task<byte[]> ExportRateLimitRulesToExcelAsync(IEnumerable<RateLimitRule> rateLimitRules, string fileName = "rate_limit_rules")
    {
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Rate Limit Rules");

            // Header
            var headers = new[]
            {
                "STT", "Tn quy tc", "Endpoint", "Phng thc", "Per pht",
                "Per gi", "Per ngy", "Trng thi", "Ngy to", "Ngy cp nht"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightYellow;
            }

            // Data
            int row = 2;
            int stt = 1;
            foreach (var rule in rateLimitRules)
            {
                worksheet.Cell(row, 1).Value = stt;
                worksheet.Cell(row, 2).Value = rule.Name;
                worksheet.Cell(row, 3).Value = rule.Endpoint;
                worksheet.Cell(row, 4).Value = rule.HttpMethod;
                worksheet.Cell(row, 5).Value = rule.RequestsPerMinute;
                worksheet.Cell(row, 6).Value = rule.RequestsPerHour;
                worksheet.Cell(row, 7).Value = rule.RequestsPerDay;
                worksheet.Cell(row, 8).Value = rule.IsActive ? "Hot ng" : "Tm dng";
                worksheet.Cell(row, 9).Value = rule.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell(row, 10).Value = rule.UpdatedAt.ToString("dd/MM/yyyy HH:mm");

                // Color code by status
                var statusColor = rule.IsActive ? XLColor.LightGreen : XLColor.LightGray;
                worksheet.Cell(row, 8).Style.Fill.BackgroundColor = statusColor;

                row++;
                stt++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add borders
            worksheet.Range(1, 1, row - 1, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(1, 1, row - 1, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting rate limit rules to Excel");
            throw;
        }
    }

    public async Task<byte[]> ExportSecurityReportToExcelAsync(DateTime? startDate = null, DateTime? endDate = null, string fileName = "security_report")
    {
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Bo co bo mt");

            // Title
            var dateRange = startDate.HasValue && endDate.HasValue
                ? $"T {startDate.Value:dd/MM/yyyy} N {endDate.Value:dd/MM/yyyy}"
                : "TT C THI GIAN";

            worksheet.Cell(1, 1).Value = $"BO CO BO MT TNG HP - {dateRange}";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range(1, 1, 1, 10).Merge();

            // Summary section
            worksheet.Cell(3, 1).Value = "TNG QUAN BO MT";
            worksheet.Cell(3, 1).Style.Font.Bold = true;
            worksheet.Cell(3, 1).Style.Font.FontSize = 14;

            var summaryHeaders = new[] { "Ch s", "Gi tr", "M t" };
            for (int i = 0; i < summaryHeaders.Length; i++)
            {
                worksheet.Cell(4, i + 1).Value = summaryHeaders[i];
                worksheet.Cell(4, i + 1).Style.Font.Bold = true;
                worksheet.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            // Sample summary data (in real implementation, this would come from database)
            var summaryData = new[]
            {
                new[] { "Tng s kin bo mt", "1,234", "S lng s kin c ghi nhn" },
                new[] { "IP b chn", "56", "S IP ang b chn" },
                new[] { "Quy tc Rate Limit", "12", "S quy tc gii hn tc " },
                new[] { "S kin Critical", "23", "S kin c mc  nghim trng cao" },
                new[] { "S kin High", "89", "S kin c mc  nghim trng trung bnh" },
                new[] { "S kin Medium", "456", "S kin c mc  nghim trng thp" },
                new[] { "S kin Low", "666", "S kin c mc  nghim trng rt thp" }
            };

            int summaryRow = 5;
            foreach (var data in summaryData)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    worksheet.Cell(summaryRow, i + 1).Value = data[i];
                }
                summaryRow++;
            }

            // Add borders for summary
            worksheet.Range(4, 1, summaryRow - 1, 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(4, 1, summaryRow - 1, 3).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting security report to Excel");
            throw;
        }
    }
}
