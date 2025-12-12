using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.Core.Services;

namespace EcommerceLaptop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class DashboardController(IAdminDashboardService dashboardService) : ControllerBase
    {
        private readonly IAdminDashboardService _dashboardService = dashboardService;

        [HttpGet("overview")]
        public async Task<ActionResult> GetDashboardOverview()
        {
            var kpis = await _dashboardService.GetDashboardKpisAsync();

            // Map to expected frontend structure
            var result = new
            {
                todaySales = new
                {
                    value = kpis.TodaySales.Value,
                    change = Math.Round(kpis.TodaySales.Change, 1),
                    isPositive = kpis.TodaySales.IsPositive
                },
                newOrders = new
                {
                    value = kpis.NewOrders.Value,
                    change = Math.Round(kpis.NewOrders.Change, 1),
                    isPositive = kpis.NewOrders.IsPositive
                },
                lowStock = new
                {
                    value = kpis.LowStock.Value,
                    change = 0,
                    isPositive = true
                },
                visitors = new
                {
                    value = kpis.Visitors.Value,
                    change = 0,
                    isPositive = true
                }
            };

            return Ok(result);
        }

        [HttpGet("sales-chart")]
        public async Task<ActionResult> GetSalesChart([FromQuery] int days = 7)
        {
            var salesDataDto = await _dashboardService.GetSalesTrendAsync(days);
            
            var salesData = salesDataDto.Select(d => new
            {
                name = d.Name, // e.g., "15/01"
                sales = d.Sales,
                orders = d.Orders
            }).ToList();

            return Ok(salesData);
        }

        [HttpGet("category-distribution")]
        public async Task<ActionResult> GetCategoryDistribution()
        {
            var categoryDataDto = await _dashboardService.GetCategoryDistributionAsync();
            
            var categoryData = categoryDataDto.Select(c => new
            {
                name = c.Name,
                value = c.Value,
                color = c.Color
            }).ToList();

            return Ok(categoryData);
        }

        [HttpGet("recent-orders")]
        public async Task<ActionResult> GetRecentOrders([FromQuery] int limit = 5)
        {
            // Service default page size is 10, we want limit
            var pagedOrders = await _dashboardService.GetRecentOrdersAsync(1, limit);
            
            var recentOrders = pagedOrders.Items.Select(o => new
            {
                id = o.Id,
                customer = o.CustomerName,
                product = o.FirstProductName,
                amount = o.Total,
                status = o.Status.ToLower(),
                time = GetTimeAgo(o.CreatedAt)
            }).ToList();

            return Ok(recentOrders);
        }

        [HttpGet("low-stock")]
        public async Task<ActionResult> GetLowStockItems([FromQuery] int limit = 10)
        {
            var lowStockDto = await _dashboardService.GetLowStockProductsAsync();
            
            var lowStockItems = lowStockDto.Take(limit).Select(i => new
            {
                name = i.Name,
                stock = i.CurrentStock,
                threshold = i.MinStock
            }).ToList();

            return Ok(lowStockItems);
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Va xong";
            else if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} pht trc";
            else if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} gi trc";
            else
                return $"{(int)timeSpan.TotalDays} ngy trc";
        }
    }
}