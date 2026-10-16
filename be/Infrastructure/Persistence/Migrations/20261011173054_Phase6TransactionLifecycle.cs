using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace be.Migrations
{
    /// <inheritdoc />
    public partial class Phase6TransactionLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryReservations_OrderId",
                table: "InventoryReservations");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "ShippingZones",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCodePrefix",
                table: "ShippingZones",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "ShippingZones",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresPhone",
                table: "ShippingZones",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresPostalCode",
                table: "ShippingZones",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresRegion",
                table: "ShippingZones",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "ShippingMethods",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "ShippingMethods",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "ShippingMethods",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedMaximumDays",
                table: "ShippingMethods",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedMinimumDays",
                table: "ShippingMethods",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDevelopmentValue",
                table: "ShippingMethods",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPickup",
                table: "ShippingMethods",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaximumOrderValue",
                table: "ShippingMethods",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumOrderValue",
                table: "ShippingMethods",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupLocation",
                table: "ShippingMethods",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Shipments",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "TrackingUrl",
                table: "Shipments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IdempotencyKey",
                table: "Refunds",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReconciledAt",
                table: "Refunds",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderReference",
                table: "Refunds",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Refunds",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<bool>(
                name: "AppliesToCustom",
                table: "Promotions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AppliesToStandard",
                table: "Promotions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Promotions",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MaximumDiscount",
                table: "Promotions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PerCustomerUsageLimit",
                table: "Promotions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Promotions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReconciledAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderStatus",
                table: "Payments",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Payments",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "TerminalAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ShippingMethodName",
                table: "Orders",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedMaximumDays",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedMinimumDays",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinalizedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPickup",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PickupLocationSnapshot",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PricesIncludeTaxSnapshot",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Orders",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "ShippingPhone",
                table: "Orders",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingRecipientName",
                table: "Orders",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingRegion",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRateSnapshot",
                table: "Orders",
                type: "decimal(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SourceCartItemId",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SourceCartUpdatedAt",
                table: "OrderItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PaymentId",
                table: "CheckoutAttempts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestFingerprint",
                table: "CheckoutAttempts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CommerceNotifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    EventKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommerceNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommerceNotifications_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentId = table.Column<long>(type: "bigint", nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProviderStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentWebhookEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentWebhookEvents_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromotionRedemptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromotionId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionRedemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionRedemptions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionRedemptions_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingZones_Country_IsActive",
                table: "ShippingZones",
                columns: new[] { "Country", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingMethods_Code",
                table: "ShippingMethods",
                column: "Code",
                unique: true,
                filter: "[Code] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_IdempotencyKey",
                table: "Refunds",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] <> '00000000-0000-0000-0000-000000000000'");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Provider_ProviderReference",
                table: "Payments",
                columns: new[] { "Provider", "ProviderReference" },
                unique: true,
                filter: "[ProviderReference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status_LastReconciledAt",
                table: "Payments",
                columns: new[] { "Status", "LastReconciledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_OrderId_ProductId_ProductVariantId",
                table: "InventoryReservations",
                columns: new[] { "OrderId", "ProductId", "ProductVariantId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_Status_ExpiresAt",
                table: "InventoryReservations",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutAttempts_PaymentId",
                table: "CheckoutAttempts",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommerceNotifications_EventKey",
                table: "CommerceNotifications",
                column: "EventKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommerceNotifications_OrderId",
                table: "CommerceNotifications",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CommerceNotifications_SentAt_LastAttemptAt",
                table: "CommerceNotifications",
                columns: new[] { "SentAt", "LastAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookEvents_PaymentId_PayloadHash",
                table: "PaymentWebhookEvents",
                columns: new[] { "PaymentId", "PayloadHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_OrderId_PromotionId",
                table: "PromotionRedemptions",
                columns: new[] { "OrderId", "PromotionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_PromotionId_UserId_Status",
                table: "PromotionRedemptions",
                columns: new[] { "PromotionId", "UserId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_CheckoutAttempts_Payments_PaymentId",
                table: "CheckoutAttempts",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckoutAttempts_Payments_PaymentId",
                table: "CheckoutAttempts");

            migrationBuilder.DropTable(
                name: "CommerceNotifications");

            migrationBuilder.DropTable(
                name: "PaymentWebhookEvents");

            migrationBuilder.DropTable(
                name: "PromotionRedemptions");

            migrationBuilder.DropIndex(
                name: "IX_ShippingZones_Country_IsActive",
                table: "ShippingZones");

            migrationBuilder.DropIndex(
                name: "IX_ShippingMethods_Code",
                table: "ShippingMethods");

            migrationBuilder.DropIndex(
                name: "IX_Refunds_IdempotencyKey",
                table: "Refunds");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Provider_ProviderReference",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Status_LastReconciledAt",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_InventoryReservations_OrderId_ProductId_ProductVariantId",
                table: "InventoryReservations");

            migrationBuilder.DropIndex(
                name: "IX_InventoryReservations_Status_ExpiresAt",
                table: "InventoryReservations");

            migrationBuilder.DropIndex(
                name: "IX_CheckoutAttempts_PaymentId",
                table: "CheckoutAttempts");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "ShippingZones");

            migrationBuilder.DropColumn(
                name: "PostalCodePrefix",
                table: "ShippingZones");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "ShippingZones");

            migrationBuilder.DropColumn(
                name: "RequiresPhone",
                table: "ShippingZones");

            migrationBuilder.DropColumn(
                name: "RequiresPostalCode",
                table: "ShippingZones");

            migrationBuilder.DropColumn(
                name: "RequiresRegion",
                table: "ShippingZones");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "ShippingMethods");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "ShippingMethods");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "ShippingMethods");

            migrationBuilder.DropColumn(
                name: "EstimatedMaximumDays",
                table: "ShippingMethods");

            migrationBuilder.DropColumn(
                name: "EstimatedMinimumDays",
                table: "ShippingMethods");

            migrationBuilder.DropColumn(
                name: "IsDevelopmentValue",
                table: "ShippingMethods");

            migrationBuilder.DropColumn(
                name: "IsPickup",
                table: "ShippingMethods");

            migrationBuilder.DropColumn(
                name: "MaximumOrderValue",
                table: "ShippingMethods");

            migrationBuilder.DropColumn(
                name: "MinimumOrderValue",
                table: "ShippingMethods");

            migrationBuilder.DropColumn(
                name: "PickupLocation",
                table: "ShippingMethods");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "TrackingUrl",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "LastReconciledAt",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "ProviderReference",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "AppliesToCustom",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "AppliesToStandard",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "MaximumDiscount",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "PerCustomerUsageLimit",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "LastReconciledAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderStatus",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "TerminalAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "EstimatedMaximumDays",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EstimatedMinimumDays",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FinalizedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsPickup",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PickupLocationSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PricesIncludeTaxSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingPhone",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingRecipientName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingRegion",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TaxRateSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SourceCartItemId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "SourceCartUpdatedAt",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "CheckoutAttempts");

            migrationBuilder.DropColumn(
                name: "RequestFingerprint",
                table: "CheckoutAttempts");

            migrationBuilder.AlterColumn<string>(
                name: "ShippingMethodName",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReservations_OrderId",
                table: "InventoryReservations",
                column: "OrderId");
        }
    }
}
