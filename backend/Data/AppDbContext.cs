using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.Role });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.Roles)
            .HasForeignKey(ur => ur.UserId);

        // Event indexes
        modelBuilder.Entity<Event>()
            .HasIndex(e => new { e.OrgId, e.UpdatedAt });

        modelBuilder.Entity<Event>()
            .HasIndex(e => new { e.OrgId, e.Category, e.Status });

        modelBuilder.Entity<Event>()
            .HasIndex(e => e.Title);

        // Enum conversions (optional – EF can store as int by default)
        modelBuilder.Entity<UserRole>()
            .Property(ur => ur.Role)
            .HasConversion<string>();

        modelBuilder.Entity<Event>()
            .Property(e => e.Status)
            .HasConversion<string>();
    }
}
