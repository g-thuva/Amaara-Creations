using be.Models;
using Microsoft.EntityFrameworkCore;

namespace be.Data;

public partial class ApplicationDbContext
{
    partial void ConfigureCustomBuilder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomBuilderConfigurationVersion>(entity =>
        {
            entity.ToTable("CustomBuilderConfigurationVersions", table =>
            {
                table.HasCheckConstraint("CK_CustomBuilderConfigurationVersions_Dimensions", "[MinimumWidth] > 0 AND [MaximumWidth] >= [MinimumWidth] AND [WidthStep] > 0 AND [MinimumHeight] > 0 AND [MaximumHeight] >= [MinimumHeight] AND [HeightStep] > 0");
                table.HasCheckConstraint("CK_CustomBuilderConfigurationVersions_Quantity", "[MinimumQuantity] > 0 AND [MaximumQuantity] >= [MinimumQuantity] AND [QuantityStep] > 0");
                table.HasCheckConstraint("CK_CustomBuilderConfigurationVersions_Pricing", "[PricePerSquareUnit] >= 0 AND [MinimumLinePrice] >= 0");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.MeasurementUnit).IsRequired().HasMaxLength(20);
            entity.Property(x => x.MinimumWidth).HasColumnType("decimal(18,2)");
            entity.Property(x => x.MaximumWidth).HasColumnType("decimal(18,2)");
            entity.Property(x => x.WidthStep).HasColumnType("decimal(18,2)");
            entity.Property(x => x.MinimumHeight).HasColumnType("decimal(18,2)");
            entity.Property(x => x.MaximumHeight).HasColumnType("decimal(18,2)");
            entity.Property(x => x.HeightStep).HasColumnType("decimal(18,2)");
            entity.Property(x => x.PricePerSquareUnit).HasColumnType("decimal(18,4)");
            entity.Property(x => x.MinimumLinePrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            entity.Property(x => x.InternalNotes).HasMaxLength(500);
            entity.Property(x => x.PublishedByUserId).HasMaxLength(450);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => x.VersionNumber).IsUnique();
            entity.HasIndex(x => new { x.Status, x.PublishedAt });
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.PublishedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CustomBuilderOption>(entity =>
        {
            entity.ToTable("CustomBuilderOptions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Group).HasConversion<int>();
            entity.Property(x => x.Code).IsRequired().HasMaxLength(80);
            entity.Property(x => x.Label).IsRequired().HasMaxLength(120);
            entity.Property(x => x.UnitPriceAdjustment).HasColumnType("decimal(18,2)");
            entity.ToTable("CustomBuilderOptions", table => table.HasCheckConstraint("CK_CustomBuilderOptions_Price", "[UnitPriceAdjustment] >= 0"));
            entity.HasIndex(x => new { x.ConfigurationVersionId, x.Group, x.Code }).IsUnique();
            entity.HasOne(x => x.ConfigurationVersion)
                .WithMany(x => x.Options)
                .HasForeignKey(x => x.ConfigurationVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomDesign>(entity =>
        {
            entity.Property(x => x.Name).IsRequired().HasMaxLength(160);
            entity.Property(x => x.Name).HasDefaultValue("Untitled custom sticker");
            entity.Property(x => x.DesignSchemaVersion).HasDefaultValue(1);
            entity.Property(x => x.ShapeCode).HasMaxLength(120);
            entity.Property(x => x.FontCode).HasMaxLength(120);
            entity.Property(x => x.ColourCode).HasMaxLength(120);
            entity.Property(x => x.CustomText).HasMaxLength(160);
            entity.Property(x => x.TextAlignment).IsRequired().HasMaxLength(20);
            entity.Property(x => x.TextAlignment).HasDefaultValue("center");
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.ProofStatus).HasConversion<int>();
            entity.Property(x => x.ProductionStatus).HasConversion<int>();
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => x.BuilderConfigurationVersionId);
            entity.HasIndex(x => new { x.UserId, x.UpdatedAt });
            entity.HasOne(x => x.BuilderConfigurationVersion)
                .WithMany()
                .HasForeignKey(x => x.BuilderConfigurationVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomDesignAsset>(entity =>
        {
            entity.Property(x => x.Width);
            entity.Property(x => x.Height);
        });

        modelBuilder.Entity<CustomDesignProofRevision>(entity =>
        {
            entity.ToTable("CustomDesignProofRevisions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.CustomerVisibleMessage).HasMaxLength(1000);
            entity.Property(x => x.CustomerResponse).HasMaxLength(1000);
            entity.Property(x => x.UploadedByUserId).HasMaxLength(450);
            entity.HasIndex(x => new { x.CustomDesignId, x.RevisionNumber }).IsUnique();
            entity.HasOne(x => x.CustomDesign)
                .WithMany(x => x.ProofRevisions)
                .HasForeignKey(x => x.CustomDesignId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Asset)
                .WithMany()
                .HasForeignKey(x => x.AssetId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItems", table => table.HasCheckConstraint("CK_CartItems_ItemType", "([ItemType] = 0 AND [ProductId] IS NOT NULL AND [CustomDesignId] IS NULL) OR ([ItemType] = 1 AND [ProductId] IS NULL AND [CustomDesignId] IS NOT NULL)"));
            entity.Property(x => x.ItemType).HasConversion<int>();
            entity.Property(x => x.UnitPriceSnapshot).HasColumnType("decimal(18,2)");
            entity.HasIndex(x => new { x.UserId, x.CustomDesignId })
                .IsUnique()
                .HasFilter("[CustomDesignId] IS NOT NULL");
            entity.HasOne(x => x.CustomDesign)
                .WithMany()
                .HasForeignKey(x => x.CustomDesignId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CustomBuilderConfigurationVersion>()
                .WithMany()
                .HasForeignKey(x => x.BuilderConfigurationVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasOne(x => x.CustomDesign)
                .WithMany()
                .HasForeignKey(x => x.CustomDesignId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
