using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EcommerceLaptop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRagSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Category", "CreatedAt", "DataType", "Description", "IsEncrypted", "SettingKey", "SettingValue", "UpdatedAt" },
                values: new object[,]
                {
                    { 100, "RAG", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "boolean", "Global toggle for RAG query rewriting feature", false, "EnableQueryRewriting", "true", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 101, "RAG", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "int", "LLM Profile ID used for query rewriting (null = disabled)", false, "ActiveRewritingProfileId", null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 102, "RAG", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "int", "Number of products to display in chat carousel", false, "ProductCarouselLimit", "5", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Category", "CreatedAt", "DataType", "Description", "IsEncrypted", "SettingKey", "SettingValue", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "RAG", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "boolean", "Global toggle for RAG query rewriting feature", false, "EnableQueryRewriting", "true", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "RAG", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "int", "LLM Profile ID used for query rewriting (null = disabled)", false, "ActiveRewritingProfileId", null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "RAG", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "int", "Number of products to display in chat carousel", false, "ProductCarouselLimit", "5", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }
    }
}
