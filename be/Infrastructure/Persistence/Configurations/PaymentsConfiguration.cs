using be.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace be.Infrastructure.Persistence.Configurations;
internal sealed class PaymentsConfiguration : IEntityTypeConfiguration<Payment>, IEntityTypeConfiguration<Refund>, IEntityTypeConfiguration<PaymentWebhookEvent>
{
    public void Configure(EntityTypeBuilder<Payment> entity)
    {
        entity.ToTable("Payments");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Provider).IsRequired().HasMaxLength(80);
        entity.Property(e => e.Method).IsRequired().HasMaxLength(80);
        entity.Property(e => e.Status).HasConversion<int>();
        entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
        entity.Property(e => e.ProviderTransactionId).HasMaxLength(160);
        entity.Property(e => e.ProviderReference).HasMaxLength(160);
        entity.Property(e => e.FailureReason).HasMaxLength(500);
        entity.HasIndex(e => e.OrderId);
        entity.HasIndex(e => e.Status);
        entity.HasIndex(e => new { e.Provider, e.ProviderTransactionId }).IsUnique().HasFilter("[ProviderTransactionId] IS NOT NULL");
        entity.HasOne(e => e.Order).WithMany(e => e.Payments).HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => new { e.Provider, e.ProviderReference }).IsUnique().HasFilter("[ProviderReference] IS NOT NULL");
        entity.HasIndex(e => new { e.Status, e.LastReconciledAt });
    }

    public void Configure(EntityTypeBuilder<Refund> entity)
    {
        entity.HasIndex(e => e.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] <> '00000000-0000-0000-0000-000000000000'");
        entity.ToTable("Refunds");
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.Payment).WithMany().HasForeignKey(e => e.PaymentId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.Order).WithMany().HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.RequestedByUser).WithMany().HasForeignKey(e => e.RequestedByUserId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<PaymentWebhookEvent> entity)
    {
        entity.HasIndex(e => new { e.PaymentId, e.PayloadHash }).IsUnique();
        entity.HasOne(e => e.Payment).WithMany().HasForeignKey(e => e.PaymentId).OnDelete(DeleteBehavior.Restrict);
    }
}
