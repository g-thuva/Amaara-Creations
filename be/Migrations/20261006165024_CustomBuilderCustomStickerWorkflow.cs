using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace be.Migrations
{
    /// <inheritdoc />
    public partial class CustomBuilderCustomStickerWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_Products_ProductId",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_CustomDesigns_CustomDesignId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_UserId_ProductId",
                table: "CartItems");

            migrationBuilder.AddColumn<int>(
                name: "CustomDesignSnapshotVersion",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BuilderConfigurationVersionId",
                table: "CustomDesigns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColourCode",
                table: "CustomDesigns",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomText",
                table: "CustomDesigns",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DesignSchemaVersion",
                table: "CustomDesigns",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "FontCode",
                table: "CustomDesigns",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "CustomDesigns",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "Untitled custom sticker");

            migrationBuilder.AddColumn<int>(
                name: "ProductionStatus",
                table: "CustomDesigns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProofStatus",
                table: "CustomDesigns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "QuotedAt",
                table: "CustomDesigns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CustomDesigns",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "ShapeCode",
                table: "CustomDesigns",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextAlignment",
                table: "CustomDesigns",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "center");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "CustomDesigns",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "CustomDesignAssets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "CustomDesignAssets",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "CartItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "BuilderConfigurationVersionId",
                table: "CartItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomDesignId",
                table: "CartItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemType",
                table: "CartItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPriceSnapshot",
                table: "CartItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CustomBuilderConfigurationVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MeasurementUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MinimumWidth = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaximumWidth = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WidthStep = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinimumHeight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaximumHeight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HeightStep = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinimumQuantity = table.Column<int>(type: "int", nullable: false),
                    MaximumQuantity = table.Column<int>(type: "int", nullable: false),
                    QuantityStep = table.Column<int>(type: "int", nullable: false),
                    PricePerSquareUnit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MinimumLinePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ArtworkRequired = table.Column<bool>(type: "bit", nullable: false),
                    ProofRequired = table.Column<bool>(type: "bit", nullable: false),
                    InternalNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomBuilderConfigurationVersions", x => x.Id);
                    table.CheckConstraint("CK_CustomBuilderConfigurationVersions_Dimensions", "[MinimumWidth] > 0 AND [MaximumWidth] >= [MinimumWidth] AND [WidthStep] > 0 AND [MinimumHeight] > 0 AND [MaximumHeight] >= [MinimumHeight] AND [HeightStep] > 0");
                    table.CheckConstraint("CK_CustomBuilderConfigurationVersions_Pricing", "[PricePerSquareUnit] >= 0 AND [MinimumLinePrice] >= 0");
                    table.CheckConstraint("CK_CustomBuilderConfigurationVersions_Quantity", "[MinimumQuantity] > 0 AND [MaximumQuantity] >= [MinimumQuantity] AND [QuantityStep] > 0");
                    table.ForeignKey(
                        name: "FK_CustomBuilderConfigurationVersions_Users_PublishedByUserId",
                        column: x => x.PublishedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CustomDesignProofRevisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomDesignId = table.Column<int>(type: "int", nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CustomerVisibleMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CustomerResponse = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CustomerResponseAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomDesignProofRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomDesignProofRevisions_CustomDesignAssets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "CustomDesignAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomDesignProofRevisions_CustomDesigns_CustomDesignId",
                        column: x => x.CustomDesignId,
                        principalTable: "CustomDesigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomDesignProofRevisions_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CustomBuilderOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConfigurationVersionId = table.Column<int>(type: "int", nullable: false),
                    Group = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    UnitPriceAdjustment = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomBuilderOptions", x => x.Id);
                    table.CheckConstraint("CK_CustomBuilderOptions_Price", "[UnitPriceAdjustment] >= 0");
                    table.ForeignKey(
                        name: "FK_CustomBuilderOptions_CustomBuilderConfigurationVersions_ConfigurationVersionId",
                        column: x => x.ConfigurationVersionId,
                        principalTable: "CustomBuilderConfigurationVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomDesigns_BuilderConfigurationVersionId",
                table: "CustomDesigns",
                column: "BuilderConfigurationVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomDesigns_UserId_UpdatedAt",
                table: "CustomDesigns",
                columns: new[] { "UserId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_BuilderConfigurationVersionId",
                table: "CartItems",
                column: "BuilderConfigurationVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CustomDesignId",
                table: "CartItems",
                column: "CustomDesignId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_UserId_CustomDesignId",
                table: "CartItems",
                columns: new[] { "UserId", "CustomDesignId" },
                unique: true,
                filter: "[CustomDesignId] IS NOT NULL");

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

            migrationBuilder.CreateIndex(
                name: "IX_CustomBuilderConfigurationVersions_PublishedByUserId",
                table: "CustomBuilderConfigurationVersions",
                column: "PublishedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomBuilderConfigurationVersions_Status_PublishedAt",
                table: "CustomBuilderConfigurationVersions",
                columns: new[] { "Status", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomBuilderConfigurationVersions_VersionNumber",
                table: "CustomBuilderConfigurationVersions",
                column: "VersionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomBuilderOptions_ConfigurationVersionId_Group_Code",
                table: "CustomBuilderOptions",
                columns: new[] { "ConfigurationVersionId", "Group", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomDesignProofRevisions_AssetId",
                table: "CustomDesignProofRevisions",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomDesignProofRevisions_CustomDesignId_RevisionNumber",
                table: "CustomDesignProofRevisions",
                columns: new[] { "CustomDesignId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomDesignProofRevisions_UploadedByUserId",
                table: "CustomDesignProofRevisions",
                column: "UploadedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_CustomBuilderConfigurationVersions_BuilderConfigurationVersionId",
                table: "CartItems",
                column: "BuilderConfigurationVersionId",
                principalTable: "CustomBuilderConfigurationVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_CustomDesigns_CustomDesignId",
                table: "CartItems",
                column: "CustomDesignId",
                principalTable: "CustomDesigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_Products_ProductId",
                table: "CartItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomDesigns_CustomBuilderConfigurationVersions_BuilderConfigurationVersionId",
                table: "CustomDesigns",
                column: "BuilderConfigurationVersionId",
                principalTable: "CustomBuilderConfigurationVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_CustomDesigns_CustomDesignId",
                table: "OrderItems",
                column: "CustomDesignId",
                principalTable: "CustomDesigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_CustomBuilderConfigurationVersions_BuilderConfigurationVersionId",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_CustomDesigns_CustomDesignId",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_Products_ProductId",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomDesigns_CustomBuilderConfigurationVersions_BuilderConfigurationVersionId",
                table: "CustomDesigns");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_CustomDesigns_CustomDesignId",
                table: "OrderItems");

            migrationBuilder.DropTable(
                name: "CustomBuilderOptions");

            migrationBuilder.DropTable(
                name: "CustomDesignProofRevisions");

            migrationBuilder.DropTable(
                name: "CustomBuilderConfigurationVersions");

            migrationBuilder.DropIndex(
                name: "IX_CustomDesigns_BuilderConfigurationVersionId",
                table: "CustomDesigns");

            migrationBuilder.DropIndex(
                name: "IX_CustomDesigns_UserId_UpdatedAt",
                table: "CustomDesigns");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_BuilderConfigurationVersionId",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CustomDesignId",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_UserId_CustomDesignId",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_UserId_ProductId",
                table: "CartItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CartItems_ItemType",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "CustomDesignSnapshotVersion",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "BuilderConfigurationVersionId",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "ColourCode",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "CustomText",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "DesignSchemaVersion",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "FontCode",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "ProductionStatus",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "ProofStatus",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "QuotedAt",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "ShapeCode",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "TextAlignment",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "CustomDesigns");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "CustomDesignAssets");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "CustomDesignAssets");

            migrationBuilder.DropColumn(
                name: "BuilderConfigurationVersionId",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "CustomDesignId",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "UnitPriceSnapshot",
                table: "CartItems");

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "CartItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_UserId_ProductId",
                table: "CartItems",
                columns: new[] { "UserId", "ProductId" });

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_Products_ProductId",
                table: "CartItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_CustomDesigns_CustomDesignId",
                table: "OrderItems",
                column: "CustomDesignId",
                principalTable: "CustomDesigns",
                principalColumn: "Id");
        }
    }
}
