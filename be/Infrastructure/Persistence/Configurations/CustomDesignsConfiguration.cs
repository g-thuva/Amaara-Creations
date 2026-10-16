using be.Domain.CustomDesigns;
using be.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace be.Infrastructure.Persistence.Configurations;
internal sealed class CustomDesignsConfiguration : IEntityTypeConfiguration<CustomDesign>, IEntityTypeConfiguration<CustomDesignAsset>, IEntityTypeConfiguration<CustomBuilderConfigurationVersion>, IEntityTypeConfiguration<CustomBuilderOption>, IEntityTypeConfiguration<CustomDesignProofRevision>
{
    public void Configure(EntityTypeBuilder<CustomDesign> entity)
    {
        entity.ToTable("CustomDesigns");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Status).HasConversion<int>();
        entity.Property(e => e.DesignJson).IsRequired();
        entity.Property(e => e.Width).HasColumnType("decimal(18,2)");
        entity.Property(e => e.Height).HasColumnType("decimal(18,2)");
        entity.Property(e => e.MaterialCode).HasMaxLength(120);
        entity.Property(e => e.FinishCode).HasMaxLength(120);
        entity.Property(e => e.CalculatedPrice).HasColumnType("decimal(18,2)");
        entity.Property(e => e.PricingRuleVersion).HasMaxLength(80);
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => e.Status);
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(e => e.PreviewMedia).WithMany().HasForeignKey(e => e.PreviewMediaId).OnDelete(DeleteBehavior.Restrict);
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
        entity.HasIndex(r => new { r.Status, r.ProductionStatus, r.ProofStatus, r.UpdatedAt });
    }

    public void Configure(EntityTypeBuilder<CustomDesignAsset> entity)
    {
        entity.ToTable("CustomDesignAssets");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.StorageKey).IsRequired().HasMaxLength(500);
        entity.Property(e => e.AssetType).IsRequired().HasMaxLength(80);
        entity.Property(e => e.OriginalFileName).HasMaxLength(255);
        entity.Property(e => e.ContentType).HasMaxLength(120);
        entity.HasIndex(e => e.CustomDesignId);
        entity.HasOne(e => e.CustomDesign).WithMany(e => e.Assets).HasForeignKey(e => e.CustomDesignId).OnDelete(DeleteBehavior.Cascade);
        entity.Property(x => x.Width);
        entity.Property(x => x.Height);
    }

    public void Configure(EntityTypeBuilder<CustomBuilderConfigurationVersion> entity)
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
    }

    public void Configure(EntityTypeBuilder<CustomBuilderOption> entity)
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
    }

    public void Configure(EntityTypeBuilder<CustomDesignProofRevision> entity)
    {
        entity.ToTable("CustomDesignProofRevisions");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Status).HasConversion<int>();
        entity.Property(x => x.CustomerVisibleMessage).HasMaxLength(1000);
        entity.Property(x => x.CustomerResponse).HasMaxLength(1000);
        entity.Property(x => x.UploadedByUserId).HasMaxLength(450);
        entity.HasIndex(x => new { x.CustomDesignId, x.RevisionNumber }).IsUnique();
        entity.HasOne(x => x.CustomDesign).WithMany(x => x.ProofRevisions).HasForeignKey(x => x.CustomDesignId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<User>().WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
