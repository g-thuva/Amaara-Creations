using be.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace be.Infrastructure.Persistence.Configurations;
internal sealed class NotificationsConfiguration : IEntityTypeConfiguration<CommerceNotification>
{
    public void Configure(EntityTypeBuilder<CommerceNotification> entity)
    {
        entity.HasIndex(e => e.EventKey).IsUnique();
        entity.HasIndex(e => new { e.SentAt, e.LastAttemptAt });
        entity.HasOne(e => e.Order).WithMany().HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
