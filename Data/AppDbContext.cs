using CVMatchAI.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CVMatchAI.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Analysis> Analyses => Set<Analysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Email único por usuario
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Relación User → Analyses (cascade delete)
        modelBuilder.Entity<Analysis>()
            .HasOne(a => a.User)
            .WithMany(u => u.Analyses)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // CvText puede ser muy largo
        modelBuilder.Entity<Analysis>()
            .Property(a => a.CvText)
            .HasMaxLength(50000);

        modelBuilder.Entity<Analysis>()
            .Property(a => a.ResultJson)
            .HasMaxLength(20000);
    }
}
