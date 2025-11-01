using Microsoft.EntityFrameworkCore;

namespace be.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets will be added here for your entities
        // Example:
        // public DbSet<Product> Products { get; set; }
        // public DbSet<User> Users { get; set; }
        // public DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure entities here
            // Example:
            // modelBuilder.Entity<Product>(entity =>
            // {
            //     entity.HasKey(e => e.Id);
            //     entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            // });
        }
    }
}

