using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceLaptop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixAdminPasswords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$J2/sXhwAhXwZR5DkjFVpquKpcb9HYY8chPihXmdO0cJfgsAG5tD8y");

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$J2/sXhwAhXwZR5DkjFVpquKpcb9HYY8chPihXmdO0cJfgsAG5tD8y");

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 3,
                column: "PasswordHash",
                value: "$2a$11$J2/sXhwAhXwZR5DkjFVpquKpcb9HYY8chPihXmdO0cJfgsAG5tD8y");

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 4,
                column: "PasswordHash",
                value: "$2a$11$J2/sXhwAhXwZR5DkjFVpquKpcb9HYY8chPihXmdO0cJfgsAG5tD8y");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$sSZ7c3ihPrgp56GKXJS7UurrOFHWs/qeI0QvgNPeUHCSpclo761Ru");

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$sSZ7c3ihPrgp56GKXJS7UurrOFHWs/qeI0QvgNPeUHCSpclo761Ru");

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 3,
                column: "PasswordHash",
                value: "$2a$11$sSZ7c3ihPrgp56GKXJS7UurrOFHWs/qeI0QvgNPeUHCSpclo761Ru");

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 4,
                column: "PasswordHash",
                value: "$2a$11$sSZ7c3ihPrgp56GKXJS7UurrOFHWs/qeI0QvgNPeUHCSpclo761Ru");
        }
    }
}
