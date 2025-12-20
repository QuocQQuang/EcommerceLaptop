using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceLaptop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveObsoleteSecurityTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.DropTable(
                name: "RateLimitViolations");

            migrationBuilder.DropTable(
                name: "SystemAuditLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminUserId = table.Column<int>(type: "int", nullable: true),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    GeoLocation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    RiskFactors = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    SuspiciousActivity = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TwoFactorRequired = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorSuccess = table.Column<bool>(type: "bit", nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    UserType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "user")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RateLimitViolations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RateLimitRuleId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "blocked"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    LimitExceeded = table.Column<int>(type: "int", nullable: false),
                    RequestCount = table.Column<int>(type: "int", nullable: false),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeWindow = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WasBlocked = table.Column<bool>(type: "bit", nullable: false),
                    WindowEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WindowStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateLimitViolations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RateLimitViolations_RateLimitRules_RateLimitRuleId",
                        column: x => x.RateLimitRuleId,
                        principalTable: "RateLimitRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminUserId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AdditionalMetadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedFields = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComplianceNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExecutionTimeMs = table.Column<int>(type: "int", nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresReview = table.Column<bool>(type: "bit", nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "low"),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UserRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemAuditLogs_Users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SystemAuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_AttemptedAt",
                table: "LoginAttempts",
                column: "AttemptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Email",
                table: "LoginAttempts",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Email_Success_AttemptedAt",
                table: "LoginAttempts",
                columns: new[] { "Email", "Success", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_IPAddress",
                table: "LoginAttempts",
                column: "IPAddress");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Success",
                table: "LoginAttempts",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitViolations_CreatedAt",
                table: "RateLimitViolations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitViolations_Endpoint",
                table: "RateLimitViolations",
                column: "Endpoint");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitViolations_IPAddress",
                table: "RateLimitViolations",
                column: "IPAddress");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitViolations_IPAddress_Endpoint_HttpMethod",
                table: "RateLimitViolations",
                columns: new[] { "IPAddress", "Endpoint", "HttpMethod" });

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitViolations_RateLimitRuleId",
                table: "RateLimitViolations",
                column: "RateLimitRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_Action",
                table: "SystemAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_AdminUserId",
                table: "SystemAuditLogs",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_CreatedAt",
                table: "SystemAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_EntityType",
                table: "SystemAuditLogs",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_EntityType_EntityId",
                table: "SystemAuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_IPAddress",
                table: "SystemAuditLogs",
                column: "IPAddress");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_UserId",
                table: "SystemAuditLogs",
                column: "UserId");
        }
    }
}
