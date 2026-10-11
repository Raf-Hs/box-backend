using DailyTracker.Domain;
using Microsoft.EntityFrameworkCore;

namespace DailyTracker.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<DailyHabitEntry> DailyHabitEntries => Set<DailyHabitEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Habit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Unit).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DailyTarget)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<DailyHabitEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Value)
                .HasPrecision(18, 2);
            entity.HasIndex(e => new { e.HabitId, e.Date })
                .IsUnique();
            entity.HasOne(e => e.Habit)
                .WithMany()
                .HasForeignKey(e => e.HabitId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
