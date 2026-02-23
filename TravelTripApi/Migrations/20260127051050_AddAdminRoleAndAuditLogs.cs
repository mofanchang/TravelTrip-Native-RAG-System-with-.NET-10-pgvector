using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TravelTripApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminRoleAndAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "admin_last_login",
                table: "accounts_customuser",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "admin_role",
                table: "accounts_customuser",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "admin_audit_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdminId = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<int>(type: "integer", nullable: false),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_admin_audit_logs_accounts_customuser_AdminId",
                        column: x => x.AdminId,
                        principalTable: "accounts_customuser",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_logs_AdminId",
                table: "admin_audit_logs",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_logs_CreatedAt",
                table: "admin_audit_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_logs_EntityType",
                table: "admin_audit_logs",
                column: "EntityType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_audit_logs");

            migrationBuilder.DropColumn(
                name: "admin_last_login",
                table: "accounts_customuser");

            migrationBuilder.DropColumn(
                name: "admin_role",
                table: "accounts_customuser");
        }
    }
}
