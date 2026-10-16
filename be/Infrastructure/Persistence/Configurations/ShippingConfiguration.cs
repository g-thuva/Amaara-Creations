using be.Domain.Shipping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace be.Infrastructure.Persistence.Configurations;
internal sealed class ShippingConfiguration : IEntityTypeConfiguration<ShippingZone>, IEntityTypeConfiguration<ShippingMethod>, IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<ShippingZone> entity)
    {
        entity.ToTable("ShippingZones");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(120);
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.HasIndex(e => e.Name).IsUnique();
        entity.HasIndex(e => new { e.Country, e.IsActive });
    }

    public void Configure(EntityTypeBuilder<ShippingMethod> entity)
    {
        entity.ToTable("ShippingMethods");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(120);
        entity.Property(e => e.BasePrice).HasColumnType("decimal(18,2)");
        entity.Property(e => e.FreeShippingThreshold).HasColumnType("decimal(18,2)");
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.HasIndex(e => new { e.ShippingZoneId, e.IsActive });
        entity.HasOne(e => e.ShippingZone).WithMany(e => e.ShippingMethods).HasForeignKey(e => e.ShippingZoneId).OnDelete(DeleteBehavior.SetNull);
        entity.HasIndex(e => e.Code).IsUnique().HasFilter("[Code] <> ''");
    }

    public void Configure(EntityTypeBuilder<Shipment> entity)
    {
        entity.ToTable("Shipments");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Method).HasMaxLength(120);
        entity.Property(e => e.Status).HasConversion<int>();
        entity.Property(e => e.Courier).HasMaxLength(120);
        entity.Property(e => e.TrackingNumber).HasMaxLength(120);
        entity.HasIndex(e => e.OrderId);
        entity.HasIndex(e => e.TrackingNumber);
        entity.HasOne(e => e.Order).WithMany(e => e.Shipments).HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
