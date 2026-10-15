using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace be.Migrations
{
    /// <inheritdoc />
    public partial class Phase7NormalizeLegacyEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing reviews remain private until an authorized moderator approves them.
            // Never infer historical verified purchase links from customer-supplied flags.
            migrationBuilder.Sql("UPDATE Reviews SET ModerationStatus = N'Pending' WHERE ModerationStatus = N'';");
            migrationBuilder.Sql("UPDATE InventoryTransactions SET MovementType = N'Adjustment' WHERE MovementType = N'';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Preserve moderation and ledger history during rollback. No data erasure.
        }
    }
}
