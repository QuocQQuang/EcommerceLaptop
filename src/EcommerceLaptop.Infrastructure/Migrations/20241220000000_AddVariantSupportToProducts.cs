using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceLaptop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantSupportToProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thm columns
            migrationBuilder.AddColumn<int>(
                name: "ParentProductId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantName",
                table: "Products",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantSku",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // Thm foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_Products_ParentProduct",
                table: "Products",
                column: "ParentProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Thm indexes
            migrationBuilder.CreateIndex(
                name: "IX_Products_ParentProductId",
                table: "Products",
                column: "ParentProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_VariantSku",
                table: "Products",
                column: "VariantSku",
                unique: true,
                filter: "[VariantSku] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_Products_VariantSku",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_ParentProductId",
                table: "Products");

            // Drop foreign key
            migrationBuilder.DropForeignKey(
                name: "FK_Products_ParentProduct",
                table: "Products");

            // Drop columns
            migrationBuilder.DropColumn(
                name: "VariantSku",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VariantName",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ParentProductId",
                table: "Products");
        }
    }
}
