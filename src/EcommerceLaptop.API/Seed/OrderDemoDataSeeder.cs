using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcommerceLaptop.API.Seed;

public static class OrderDemoDataSeeder
{
    public static void SeedIfEmpty(ApplicationDbContext context, ILogger logger)
    {
        var hasAnyOrders = context.Orders.Any();
        if (hasAnyOrders)
        {
            return;
        }

        var random = new Random(12345);

        // 1) Ensure customer users (exclude existing admin id=1)
        var customers = context.Users.Where(u => u.Id != 1).ToList();
        if (customers.Count < 20)
        {
            var toCreate = 20 - customers.Count;
            for (var i = 0; i < toCreate; i++)
            {
                var idx = customers.Count + i + 1;
                context.Users.Add(new User
                {
                    Email = $"customer{idx}@example.com",
                    PasswordHash = "$2a$12$K4r7dDD9BpGz.NlGfQnnPOeBjeSx/ALRb4o54eAFPX.wbdpSTqYEi", // same as admin seed (BCrypt of Admin123!)
                    FirstName = "Customer",
                    LastName = idx.ToString(),
                    PhoneNumber = $"+8490{random.Next(1000000, 9999999)}",
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 120)),
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                });
            }
            context.SaveChanges();
            customers = context.Users.Where(u => u.Id != 1).ToList();
        }

        // 2) Ensure inventory exists for all products
        var productIds = context.Products.Select(p => p.Id).ToList();
        var inventoryByProduct = context.Inventories.AsNoTracking().ToDictionary(i => i.ProductId, i => i);
        foreach (var pid in productIds)
        {
            if (!inventoryByProduct.ContainsKey(pid))
            {
                context.Inventories.Add(new Inventory
                {
                    ProductId = pid,
                    QuantityInStock = 100,
                    ReservedQuantity = 0,
                    ReorderLevel = 10,
                    MaxStockLevel = 200,
                    WarehouseLocation = "WH-SEED",
                    LastStockUpdate = DateTime.UtcNow
                });
            }
        }
        context.SaveChanges();

        // Reload inventory for mutation
        var inventories = context.Inventories.ToDictionary(i => i.ProductId, i => i);

        // 3) Create 100 orders over last 30 days
        var products = context.Products.AsNoTracking().ToList();
        if (products.Count == 0)
        {
            logger.LogWarning("No products found to seed orders.");
            return;
        }

        var orderCount = 100;
        var ordersToAdd = new List<Order>(orderCount);
        var orderItemsToAdd = new List<OrderItem>(orderCount * 2);

        for (var i = 0; i < orderCount; i++)
        {
            var customer = customers[random.Next(customers.Count)];
            var createdAt = DateTime.UtcNow.AddDays(-random.Next(0, 30)).AddMinutes(-random.Next(0, 1440));

            // Choose status with bias toward Confirmed/Shipped
            var statusRoll = random.NextDouble();
            var status = statusRoll < 0.7 ? OrderStatus.Confirmed : statusRoll < 0.9 ? OrderStatus.Shipped : OrderStatus.Processing;

            var itemCount = random.Next(1, 4);
            var chosenProducts = products.OrderBy(_ => random.Next()).Take(itemCount).ToList();

            decimal subTotal = 0m;
            foreach (var p in chosenProducts)
            {
                var qty = random.Next(1, 3);
                var unitPrice = p.Price > 0 ? p.Price : random.Next(500_000, 5_000_000);
                var itemTotal = unitPrice * qty;
                subTotal += itemTotal;
            }

            var tax = Math.Round(subTotal * 0.08m, 2);
            var shipping = subTotal > 5_000_000m ? 0 : 50_000m;
            var discount = 0m;
            var total = subTotal + tax + shipping - discount;

            var order = new Order
            {
                UserId = customer.Id,
                OrderNumber = $"SEED-{DateTime.UtcNow:yyyyMMdd}-{i + 1:0000}",
                Status = status,
                OrderDate = createdAt,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                SubTotal = subTotal,
                TaxAmount = tax,
                ShippingAmount = shipping,
                DiscountAmount = discount,
                TotalAmount = total,
                InventoryReserved = status != OrderStatus.Cancelled,
                ShippingStreet = "123 Seed Street",
                ShippingCity = "Hanoi",
                ShippingProvince = "HN",
                ShippingPostalCode = "100000",
                ShippingCountry = "VN"
            };
            ordersToAdd.Add(order);
        }

        context.Orders.AddRange(ordersToAdd);
        context.SaveChanges();

        // Create items and decrement inventory
        var persistedOrders = context.Orders.OrderByDescending(o => o.Id).Take(orderCount).ToList();
        var index = 0;
        foreach (var order in persistedOrders)
        {
            var rnd = new Random(12345 + index++);
            var itemCount = rnd.Next(1, 4);
            var chosenProducts = products.OrderBy(_ => rnd.Next()).Take(itemCount).ToList();

            foreach (var p in chosenProducts)
            {
                var qty = rnd.Next(1, 3);
                var unitPrice = p.Price > 0 ? p.Price : rnd.Next(500_000, 5_000_000);
                var totalPrice = unitPrice * qty;

                orderItemsToAdd.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = p.Id,
                    Quantity = qty,
                    UnitPrice = unitPrice,
                    DiscountAmount = 0,
                    TotalPrice = totalPrice
                });

                if (inventories.TryGetValue(p.Id, out var inv))
                {
                    inv.QuantityInStock = Math.Max(0, inv.QuantityInStock - qty);
                    inv.ReservedQuantity += 0;
                    inv.LastStockUpdate = DateTime.UtcNow;
                }
            }
        }

        context.OrderItems.AddRange(orderItemsToAdd);
        context.SaveChanges();

        logger.LogInformation("Seeded {OrderCount} demo orders with items and ensured inventory for all products.", orderCount);
    }
}


