using be.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace be.Infrastructure.Persistence.Configurations;
internal sealed class AuditConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> entity)
    {
        entity.ToTable("AuditLogs");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Action).IsRequired().HasMaxLength(120);
        entity.Property(e => e.EntityType).IsRequired().HasMaxLength(120);
        entity.Property(e => e.EntityId).HasMaxLength(120);
        entity.Property(e => e.CorrelationId).HasMaxLength(100);
        entity.Property(e => e.IpAddress).HasMaxLength(64);
        entity.HasIndex(e => new { e.EntityType, e.EntityId });
        entity.HasIndex(e => e.ActorUserId);
        entity.HasIndex(e => e.CreatedAt);
        entity.HasOne(e => e.ActorUser).WithMany().HasForeignKey(e => e.ActorUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
