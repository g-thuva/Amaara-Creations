using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace be.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdminCustomerLifecycleAndDirectReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountStatus",
                table: "Users",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<DateTime>(
                name: "AccountStatusChangedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountStatusReason",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ModerationStatus",
                table: "Reviews",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Published",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Pending");

            migrationBuilder.CreateIndex(
                name: "IX_Users_AccountStatus_CreatedAt",
                table: "Users",
                columns: new[] { "AccountStatus", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // A rollback must not silently discard administrative restrictions.
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [Users] WHERE [AccountStatus] <> N'Active' OR [AccountStatusReason] IS NOT NULL) THROW 51004, 'Review customer access decisions before downgrading the account lifecycle schema.', 1;");
            migrationBuilder.DropIndex(
                name: "IX_Users_AccountStatus_CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AccountStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AccountStatusChangedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AccountStatusReason",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "ModerationStatus",
                table: "Reviews",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Published");
        }
    }
}
