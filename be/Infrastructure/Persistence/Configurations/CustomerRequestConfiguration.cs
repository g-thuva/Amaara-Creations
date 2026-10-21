using be.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace be.Infrastructure.Persistence.Configurations;
internal sealed class CustomerRequestConfiguration : IEntityTypeConfiguration<CustomerRequest>
{
    public void Configure(EntityTypeBuilder<CustomerRequest> entity)
    {
        entity.ToTable("CustomerRequests");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Subject).IsRequired().HasMaxLength(160);
        entity.Property(x => x.Details).IsRequired().HasMaxLength(2000);
        entity.Property(x => x.Reply).HasMaxLength(2000);
        entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.CustomDesign).WithMany().HasForeignKey(x => x.CustomDesignId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => new { x.UserId, x.CreatedAt });
        entity.HasIndex(x => new { x.Kind, x.Status, x.CreatedAt });
    }
}
