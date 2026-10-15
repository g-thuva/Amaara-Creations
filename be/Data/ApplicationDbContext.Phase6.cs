using Microsoft.EntityFrameworkCore;
using be.Models;

namespace be.Data
{
    public partial class ApplicationDbContext
    {
        partial void ConfigurePhase6(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>().HasIndex(e => new { e.Provider, e.ProviderReference }).IsUnique().HasFilter("[ProviderReference] IS NOT NULL");
            modelBuilder.Entity<Payment>().HasIndex(e => new { e.Status, e.LastReconciledAt });
            modelBuilder.Entity<ShippingZone>().HasIndex(e => new { e.Country, e.IsActive });
            modelBuilder.Entity<ShippingMethod>().HasIndex(e => e.Code).IsUnique().HasFilter("[Code] <> ''");
            modelBuilder.Entity<InventoryReservation>().HasIndex(e => new { e.Status, e.ExpiresAt });
            modelBuilder.Entity<InventoryReservation>().HasIndex(e => new { e.OrderId, e.ProductId, e.ProductVariantId });
            modelBuilder.Entity<Refund>().HasIndex(e => e.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] <> '00000000-0000-0000-0000-000000000000'");
            modelBuilder.Entity<CheckoutAttempt>().HasOne(e => e.Payment).WithMany().HasForeignKey(e => e.PaymentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PromotionRedemption>(entity =>
            {
                entity.HasIndex(e => new { e.OrderId, e.PromotionId }).IsUnique();
                entity.HasIndex(e => new { e.PromotionId, e.UserId, e.Status });
                entity.HasOne(e => e.Order).WithMany().HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Promotion).WithMany().HasForeignKey(e => e.PromotionId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<PaymentWebhookEvent>(entity =>
            {
                entity.HasIndex(e => new { e.PaymentId, e.PayloadHash }).IsUnique();
                entity.HasOne(e => e.Payment).WithMany().HasForeignKey(e => e.PaymentId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<CommerceNotification>(entity =>
            {
                entity.HasIndex(e => e.EventKey).IsUnique();
                entity.HasIndex(e => new { e.SentAt, e.LastAttemptAt });
                entity.HasOne(e => e.Order).WithMany().HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<InventoryReservation>(entity =>
            {
                entity.ToTable("InventoryReservations");
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Order)
                    .WithMany()
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ProductVariant)
                    .WithMany()
                    .HasForeignKey(e => e.ProductVariantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Refund>(entity =>
            {
                entity.ToTable("Refunds");
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Payment)
                    .WithMany()
                    .HasForeignKey(e => e.PaymentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Order)
                    .WithMany()
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.RequestedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.RequestedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<CheckoutAttempt>(entity =>
            {
                entity.ToTable("CheckoutAttempts");
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Order)
                    .WithMany()
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
