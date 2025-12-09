using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;

namespace EcommerceLaptop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("overview")]
        public async Task<ActionResult> GetDashboardOverview()
        {
            try
            {
                var today = DateTime.Today;
                var yesterday = today.AddDays(-1);
                var lastWeek = today.AddDays(-7);
                var lastMonth = today.AddMonths(-1);

                // Today's sales
                var todaySales = await _context.Orders
                    .Where(o => o.CreatedAt.Date == today && o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => o.TotalAmount);

                var yesterdaySales = await _context.Orders
                    .Where(o => o.CreatedAt.Date == yesterday && o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => o.TotalAmount);

                var salesChange = yesterdaySales > 0 ? ((todaySales - yesterdaySales) / yesterdaySales) * 100 : 0;

                // New orders today
                var todayOrders = await _context.Orders
                    .Where(o => o.CreatedAt.Date == today)
                    .CountAsync();

                var yesterdayOrders = await _context.Orders
                    .Where(o => o.CreatedAt.Date == yesterday)
                    .CountAsync();

                var ordersChange = yesterdayOrders > 0 ? ((double)(todayOrders - yesterdayOrders) / yesterdayOrders) * 100 : 0;

                // Low stock items - need to check what properties Inventory actually has
                var lowStockItems = await _context.Inventories
                    .Where(i => i.QuantityInStock <= i.ReorderLevel)
                    .CountAsync();

                // Active users (users created recently as proxy for activity)
                var activeUsers = await _context.Users
                    .Where(u => u.IsActive && u.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                    .CountAsync();

                var result = new
                {
                    todaySales = new
                    {
                        value = todaySales,
                        change = Math.Round(salesChange, 1),
                        isPositive = salesChange >= 0
                    },
                    newOrders = new
                    {
                        value = todayOrders,
                        change = Math.Round(ordersChange, 1),
                        isPositive = ordersChange >= 0
                    },
                    lowStock = new
                    {
                        value = lowStockItems,
                        change = 0,
                        isPositive = true
                    },
                    visitors = new
                    {
                        value = activeUsers,
                        change = 0,
                        isPositive = true
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Li khi ly d liu dashboard", error = ex.Message });
            }
        }

        [HttpGet("sales-chart")]
        public async Task<ActionResult> GetSalesChart([FromQuery] int days = 7)
        {
            try
            {
                var endDate = DateTime.Today;
                var startDate = endDate.AddDays(-days + 1);

                var salesData = new List<object>();

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var dailySales = await _context.Orders
                        .Where(o => o.CreatedAt.Date == date && o.Status != OrderStatus.Cancelled)
                        .SumAsync(o => o.TotalAmount);

                    var dailyOrders = await _context.Orders
                        .Where(o => o.CreatedAt.Date == date)
                        .CountAsync();

                    salesData.Add(new
                    {
                        name = date.ToString("dd/MM"),
                        sales = dailySales,
                        orders = dailyOrders
                    });
                }

                return Ok(salesData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Li khi ly d liu biu ", error = ex.Message });
            }
        }

        [HttpGet("category-distribution")]
        public async Task<ActionResult> GetCategoryDistribution()
        {
            try
            {
                var categoryData = await _context.Orders
                    .Where(o => o.Status != OrderStatus.Cancelled)
                    .SelectMany(o => o.OrderItems)
                    .GroupBy(oi => oi.Product.GetType().Name)
                    .Select(g => new
                    {
                        name = g.Key == "Laptop" ? "Laptop" : 
                               g.Key == "Accessory" ? "Ph kin" : "Khc",
                        value = g.Count(),
                        color = g.Key == "Laptop" ? "#0088FE" : 
                                g.Key == "Accessory" ? "#00C49F" : "#FFBB28"
                    })
                    .ToListAsync();

                return Ok(categoryData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Li khi ly phn b danh mc", error = ex.Message });
            }
        }

        [HttpGet("recent-orders")]
        public async Task<ActionResult> GetRecentOrders([FromQuery] int limit = 5)
        {
            try
            {
                var recentOrders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(limit)
                    .Select(o => new
                    {
                        id = o.Id,
                        customer = $"{o.User.FirstName} {o.User.LastName}",
                        product = o.OrderItems.First().Product.Name,
                        amount = o.TotalAmount,
                        status = o.Status.ToString().ToLower(),
                        time = GetTimeAgo(o.CreatedAt)
                    })
                    .ToListAsync();

                return Ok(recentOrders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Li khi ly n hng gn y", error = ex.Message });
            }
        }

        [HttpGet("low-stock")]
        public async Task<ActionResult> GetLowStockItems([FromQuery] int limit = 10)
        {
            try
            {
                var lowStockItems = await _context.Inventories
                    .Include(i => i.Product)
                    .Where(i => i.QuantityInStock <= i.ReorderLevel)
                    .OrderBy(i => i.QuantityInStock)
                    .Take(limit)
                    .Select(i => new
                    {
                        name = i.Product.Name,
                        stock = i.QuantityInStock,
                        threshold = i.ReorderLevel
                    })
                    .ToListAsync();

                return Ok(lowStockItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Li khi ly sn phm ht hng", error = ex.Message });
            }
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