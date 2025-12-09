using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceLaptop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminPasswords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$12$K4r7dDD9BpGz.NlGfQnnPOeBjeSx/ALRb4o54eAFPX.wbdpSTqYEi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$rqiEwJ5k3qV4y5e5KqT4k.8UX9g2XKJt7f4y5e5KqT4k8UX9g2XKJ");

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$rqiEwJ5k3qV4y5e5KqT4k.8UX9g2XKJt7f4y5e5KqT4k8UX9g2XKJ");

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 3,
                column: "PasswordHash",
                value: "$2a$11$rqiEwJ5k3qV4y5e5KqT4k.8UX9g2XKJt7f4y5e5KqT4k8UX9g2XKJ");

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 4,
                column: "PasswordHash",
                value: "$2a$11$rqiEwJ5k3qV4y5e5KqT4k.8UX9g2XKJt7f4y5e5KqT4k8UX9g2XKJ");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$12$E4JLQhnSHWye5rV2Fb13tORcco3l1bTwU1FszCA59j.M7Kzk8LeXq");
        }
    }
}
