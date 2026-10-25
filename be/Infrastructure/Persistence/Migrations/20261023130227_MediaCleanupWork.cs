using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace be.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MediaCleanupWork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaCleanupWork",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PublicUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NotBeforeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastAttemptUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaCleanupWork", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaCleanupWork_NotBeforeUtc",
                table: "MediaCleanupWork",
                column: "NotBeforeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MediaCleanupWork_StorageKey",
                table: "MediaCleanupWork",
                column: "StorageKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaCleanupWork");
        }
    }
}
