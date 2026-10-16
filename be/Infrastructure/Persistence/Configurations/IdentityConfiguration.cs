using be.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace be.Infrastructure.Persistence.Configurations;
internal sealed class IdentityConfiguration : IEntityTypeConfiguration<User>, IEntityTypeConfiguration<IdentityRole>, IEntityTypeConfiguration<IdentityUserRole<string>>, IEntityTypeConfiguration<IdentityUserClaim<string>>, IEntityTypeConfiguration<IdentityUserLogin<string>>, IEntityTypeConfiguration<IdentityRoleClaim<string>>, IEntityTypeConfiguration<IdentityUserToken<string>>, IEntityTypeConfiguration<Address>, IEntityTypeConfiguration<RefreshSession>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.ToTable("Users");
    }

    public void Configure(EntityTypeBuilder<IdentityRole> entity)
    {
        entity.ToTable("Roles");
    }

    public void Configure(EntityTypeBuilder<IdentityUserRole<string>> entity)
    {
        entity.ToTable("UserRoles");
    }

    public void Configure(EntityTypeBuilder<IdentityUserClaim<string>> entity)
    {
        entity.ToTable("UserClaims");
    }

    public void Configure(EntityTypeBuilder<IdentityUserLogin<string>> entity)
    {
        entity.ToTable("UserLogins");
    }

    public void Configure(EntityTypeBuilder<IdentityRoleClaim<string>> entity)
    {
        entity.ToTable("RoleClaims");
    }

    public void Configure(EntityTypeBuilder<IdentityUserToken<string>> entity)
    {
        entity.ToTable("UserTokens");
    }

    public void Configure(EntityTypeBuilder<Address> entity)
    {
        entity.ToTable("Addresses");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Label).IsRequired().HasMaxLength(80);
        entity.Property(e => e.RecipientName).IsRequired().HasMaxLength(120);
        entity.Property(e => e.Phone).HasMaxLength(40);
        entity.Property(e => e.AddressLine1).IsRequired().HasMaxLength(200);
        entity.Property(e => e.AddressLine2).HasMaxLength(200);
        entity.Property(e => e.City).IsRequired().HasMaxLength(100);
        entity.Property(e => e.DistrictOrProvince).HasMaxLength(100);
        entity.Property(e => e.PostalCode).HasMaxLength(30);
        entity.Property(e => e.Country).IsRequired().HasMaxLength(80);
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => new { e.UserId, e.IsDefault });
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<RefreshSession> entity)
    {
        entity.ToTable("RefreshSessions");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(128);
        entity.Property(e => e.FamilyId).IsRequired().HasMaxLength(64);
        entity.Property(e => e.UserAgent).HasMaxLength(300);
        entity.Property(e => e.IpAddress).HasMaxLength(64);
        entity.HasIndex(e => e.TokenHash).IsUnique();
        entity.HasIndex(e => new { e.UserId, e.FamilyId });
        entity.HasIndex(e => e.ExpiresAt);
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.ReplacedBySession).WithMany().HasForeignKey(e => e.ReplacedBySessionId).OnDelete(DeleteBehavior.Restrict);
    }
}
