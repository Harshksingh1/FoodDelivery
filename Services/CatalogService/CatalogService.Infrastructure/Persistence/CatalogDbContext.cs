using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Persistence;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<RestaurantApplication> RestaurantApplications => Set<RestaurantApplication>();
    public DbSet<DeliveryAgentApplication> DeliveryAgentApplications => Set<DeliveryAgentApplication>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Restaurant>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.MinOrderAmount).HasColumnType("decimal(18,2)");
            e.HasMany(r => r.MenuItems).WithOne(m => m.Restaurant).HasForeignKey(m => m.RestaurantId);
        });

        b.Entity<MenuItem>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Price).HasColumnType("decimal(18,2)");
        });

        b.Entity<RestaurantApplication>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Status).HasConversion<string>();
        });

        b.Entity<DeliveryAgentApplication>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Status).HasConversion<string>();
        });
    }
}
