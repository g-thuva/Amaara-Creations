using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace be.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistentSessionPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AbsoluteExpiresAt",
                table: "RefreshSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FamilyCreatedAt",
                table: "RefreshSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IdleExpiresAt",
                table: "RefreshSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityAt",
                table: "RefreshSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Management",
                table: "RefreshSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MfaVerifiedAt",
                table: "RefreshSessions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Privileged",
                table: "RefreshSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RevocationReason",
                table: "RefreshSessions",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RolesHash",
                table: "RefreshSessions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStampHash",
                table: "RefreshSessions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbsoluteExpiresAt",
                table: "RefreshSessions");

            migrationBuilder.DropColumn(
                name: "FamilyCreatedAt",
                table: "RefreshSessions");

            migrationBuilder.DropColumn(
                name: "IdleExpiresAt",
                table: "RefreshSessions");

            migrationBuilder.DropColumn(
                name: "LastActivityAt",
                table: "RefreshSessions");

            migrationBuilder.DropColumn(
                name: "Management",
                table: "RefreshSessions");

            migrationBuilder.DropColumn(
                name: "MfaVerifiedAt",
                table: "RefreshSessions");

            migrationBuilder.DropColumn(
                name: "Privileged",
                table: "RefreshSessions");

            migrationBuilder.DropColumn(
                name: "RevocationReason",
                table: "RefreshSessions");

            migrationBuilder.DropColumn(
                name: "RolesHash",
                table: "RefreshSessions");

            migrationBuilder.DropColumn(
                name: "SecurityStampHash",
                table: "RefreshSessions");
        }
    }
}
