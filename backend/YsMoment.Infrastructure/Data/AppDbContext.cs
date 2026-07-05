using Microsoft.EntityFrameworkCore;
using YsMoment.Core.Entities;

namespace YsMoment.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.HostNames).HasMaxLength(300);
            e.Property(x => x.Slug).HasMaxLength(50);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.EventId, x.OrderNumber }).IsUnique();
            e.HasIndex(x => x.PublicToken).IsUnique();
            e.HasIndex(x => new { x.EventId, x.Phone });
            e.Property(x => x.PublicToken).HasMaxLength(64);
            e.Property(x => x.CustomerName).HasMaxLength(150);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.HasOne(x => x.Event).WithMany(x => x.Orders).HasForeignKey(x => x.EventId);
        });
    }
}
