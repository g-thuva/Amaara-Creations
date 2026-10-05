using be.Data;
using be.Models;
using Microsoft.EntityFrameworkCore;

namespace be.Tests
{
    public class ApplicationDbContextModelTests
    {
        [Fact]
        public void Phase1FoundationEntitiesAreMapped()
        {
            using var context = CreateContext();
            var entityNames = context.Model.GetEntityTypes().Select(e => e.ClrType.Name).ToHashSet();

            Assert.Contains(nameof(Category), entityNames);
            Assert.Contains(nameof(ProductVariant), entityNames);
            Assert.Contains(nameof(ProductMedia), entityNames);
            Assert.Contains(nameof(Address), entityNames);
            Assert.Contains(nameof(CustomDesign), entityNames);
            Assert.Contains(nameof(RefreshSession), entityNames);
            Assert.Contains(nameof(Payment), entityNames);
            Assert.Contains(nameof(Shipment), entityNames);
            Assert.Contains(nameof(Promotion), entityNames);
            Assert.Contains(nameof(AuditLog), entityNames);
        }

        [Fact]
        public void Phase3CatalogueCmsEntitiesAreMapped()
        {
            using var context = CreateContext();
            var entityNames = context.Model.GetEntityTypes().Select(e => e.ClrType.Name).ToHashSet();

            Assert.Contains(nameof(InventoryTransaction), entityNames);
            Assert.Contains(nameof(CmsPage), entityNames);
            Assert.Contains(nameof(CmsSection), entityNames);
            Assert.Contains(nameof(SiteSetting), entityNames);
            Assert.NotNull(context.Model.FindEntityType(typeof(ProductMedia))!.FindProperty(nameof(ProductMedia.StorageProvider)));
            Assert.NotNull(context.Model.FindEntityType(typeof(ProductMedia))!.FindProperty(nameof(ProductMedia.OriginalFileName)));
        }

        [Fact]
        public void ProductVariantUsesRowVersionConcurrency()
        {
            using var context = CreateContext();
            var rowVersion = context.Model.FindEntityType(typeof(ProductVariant))!
                .FindProperty(nameof(ProductVariant.RowVersion))!;

            Assert.True(rowVersion.IsConcurrencyToken);
            Assert.Equal("rowversion", rowVersion.GetColumnType());
        }

        [Fact]
        public void OrderItemSupportsImmutableSnapshotFields()
        {
            using var context = CreateContext();
            var orderItem = context.Model.FindEntityType(typeof(OrderItem))!;

            Assert.NotNull(orderItem.FindProperty(nameof(OrderItem.ProductNameSnapshot)));
            Assert.NotNull(orderItem.FindProperty(nameof(OrderItem.UnitPrice)));
            Assert.NotNull(orderItem.FindProperty(nameof(OrderItem.ImageSnapshot)));
            Assert.True(orderItem.FindProperty(nameof(OrderItem.ProductId))!.IsNullable);
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=AmaaraModelOnly;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
