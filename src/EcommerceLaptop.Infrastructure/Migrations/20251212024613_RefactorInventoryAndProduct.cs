using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceLaptop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorInventoryAndProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.AddColumn<string>(
            //     name: "Reason",
            //     table: "InventoryTransactions",
            //     type: "nvarchar(max)",
            //     nullable: false,
            //     defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reason",
                table: "InventoryTransactions");
        }
    }
}
