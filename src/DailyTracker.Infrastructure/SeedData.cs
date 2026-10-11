using DailyTracker.Domain;
using Microsoft.EntityFrameworkCore;

namespace DailyTracker.Infrastructure;

public static class SeedData
{
    public static readonly (string Name, string Unit, decimal Target)[] DefaultHabits =
    [
        ("Sleep", "Hours", 8m),
        ("Food", "Meals", 3m),
        ("Water", "Glasses", 8m),
        ("Walk", "Steps", 10000m),
        ("Sun", "Minutes", 10m),
        ("Stand up", "Breaks", 4m),
    ];

    public static async Task SeedHabitsAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Habits.AnyAsync(ct))
            return;

        var habits = DefaultHabits.Select(h => new Habit
        {
            Name = h.Name,
            Unit = h.Unit,
            DailyTarget = h.Target,
            IsActive = true,
        });

        await db.Habits.AddRangeAsync(habits, ct);
        await db.SaveChangesAsync(ct);
    }
}
