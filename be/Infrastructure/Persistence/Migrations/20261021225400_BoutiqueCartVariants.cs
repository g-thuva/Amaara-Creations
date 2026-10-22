using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace be.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BoutiqueCartVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CartItems_UserId_ProductId",
                table: "CartItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CartItems_ItemType",
                table: "CartItems");

            migrationBuilder.AddColumn<int>(
                name: "ProductVariantId",
                table: "CartItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductVariantId",
                table: "CartItems",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_UserId_ProductId_ProductVariantId",
                table: "CartItems",
                columns: new[] { "UserId", "ProductId", "ProductVariantId" },
                unique: true,
                filter: "[ProductId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CartItems_ItemType",
                table: "CartItems",
                sql: "([ItemType] = 0 AND [ProductId] IS NOT NULL AND [CustomDesignId] IS NULL) OR ([ItemType] = 1 AND [ProductId] IS NULL AND [ProductVariantId] IS NULL AND [CustomDesignId] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_ProductVariants_ProductVariantId",
                table: "CartItems",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // A rollback must not silently discard a buyer's selected option.
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [CartItems] WHERE [ProductVariantId] IS NOT NULL) THROW 51003, 'Variant cart lines must be resolved before rolling back this migration.', 1;");
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_ProductVariants_ProductVariantId",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_ProductVariantId",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_UserId_ProductId_ProductVariantId",
                table: "CartItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CartItems_ItemType",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "CartItems");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_UserId_ProductId",
                table: "CartItems",
                columns: new[] { "UserId", "ProductId" },
                unique: true,
                filter: "[ProductId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CartItems_ItemType",
                table: "CartItems",
                sql: "([ItemType] = 0 AND [ProductId] IS NOT NULL AND [CustomDesignId] IS NULL) OR ([ItemType] = 1 AND [ProductId] IS NULL AND [CustomDesignId] IS NOT NULL)");
        }
    }
}
