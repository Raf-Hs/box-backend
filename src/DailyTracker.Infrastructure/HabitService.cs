using DailyTracker.Application;
using DailyTracker.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DailyTracker.Infrastructure;

public class HabitService(AppDbContext db) : IHabitService
{
    public async Task<IReadOnlyList<HabitDto>> GetActiveHabitsAsync(CancellationToken ct = default)
    {
        return await db.Habits
            .Where(h => h.IsActive)
            .OrderBy(h => h.Id)
            .Select(h => new HabitDto(h.Id, h.Name, h.Unit, h.DailyTarget, h.IsActive))
            .ToListAsync(ct);
    }

    public async Task<HabitDto?> GetHabitAsync(int id, CancellationToken ct = default)
    {
        return await db.Habits
            .Where(h => h.IsActive && h.Id == id)
            .Select(h => new HabitDto(h.Id, h.Name, h.Unit, h.DailyTarget, h.IsActive))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<HabitDto> UpdateTargetAsync(int id, decimal newTarget, CancellationToken ct = default)
    {
        if (newTarget < 0)
            throw new ArgumentException("Target cannot be negative.", nameof(newTarget));

        var habit = await db.Habits.FirstOrDefaultAsync(h => h.Id == id && h.IsActive, ct)
            ?? throw new KeyNotFoundException($"Habit {id} not found.");

        habit.UpdateTarget(newTarget);
        await db.SaveChangesAsync(ct);

        return new HabitDto(habit.Id, habit.Name, habit.Unit, habit.DailyTarget, habit.IsActive);
    }
}
