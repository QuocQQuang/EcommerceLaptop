using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EcommerceLaptop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedBundleSeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Brand", "CreatedAt", "Description", "IsActive", "Model", "Name", "Price", "SKU", "UpdatedAt" },
                values: new object[] { 100, "TechStore", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Complete gaming setup with Dell XPS 13 Plus and essential accessories", true, "STARTER-001", "Gaming Starter Bundle", 1399.99m, "BUNDLE-STARTER-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Bundles",
                columns: new[] { "Id", "BundleType", "DiscountPercentage", "ValidFrom", "ValidTo" },
                values: new object[] { 100, "Gaming", 15.0m, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 10, 12, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Inventories",
                columns: new[] { "Id", "LastStockUpdate", "MaxStockLevel", "ProductId", "QuantityInStock", "ReorderLevel", "ReservedQuantity", "WarehouseLocation" },
                values: new object[] { 100, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 20, 100, 10, 2, 0, "WH-BUNDLE-001" });

            migrationBuilder.InsertData(
                table: "BundleItems",
                columns: new[] { "Id", "BundleId", "DiscountPercentage", "ProductId", "Quantity" },
                values: new object[,]
                {
                    { -2, 100, 20.0m, 3, 1 },
                    { -1, 100, 10.0m, 1, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BundleItems",
                keyColumn: "Id",
                keyValue: -2);

            migrationBuilder.DeleteData(
                table: "BundleItems",
                keyColumn: "Id",
                keyValue: -1);

            migrationBuilder.DeleteData(
                table: "Inventories",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "Bundles",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 100);
        }
    }
}
