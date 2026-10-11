using DailyTracker.Application;
using DailyTracker.Application.Dtos;
using DailyTracker.Domain;
using Microsoft.EntityFrameworkCore;

namespace DailyTracker.Infrastructure;

public class DailyEntryService(AppDbContext db) : IDailyEntryService
{
    public async Task<IReadOnlyList<DailyEntryDto>> GetEntriesForDateAsync(DateTime date, CancellationToken ct = default)
    {
        var habits = await db.Habits
            .Where(h => h.IsActive)
            .OrderBy(h => h.Id)
            .ToListAsync(ct);

        var entries = await db.DailyHabitEntries
            .Where(e => e.Date == date)
            .ToListAsync(ct);

        var entryDict = entries.ToDictionary(e => e.HabitId, e => e.Value);

        return habits.Select(h => new DailyEntryDto(
            h.Id,
            h.Name,
            h.Unit,
            date,
            entryDict.GetValueOrDefault(h.Id, 0m),
            h.DailyTarget
        )).ToList();
    }

    public async Task<DailyEntryDto> SetEntryValueAsync(int habitId, DateTime date, decimal value, CancellationToken ct = default)
    {
        if (value < 0)
            throw new ArgumentException("Value cannot be negative.", nameof(value));

        var habit = await db.Habits.FirstOrDefaultAsync(h => h.Id == habitId && h.IsActive, ct)
            ?? throw new KeyNotFoundException($"Habit {habitId} not found.");

        var entry = await db.DailyHabitEntries
            .FirstOrDefaultAsync(e => e.HabitId == habitId && e.Date == date, ct);

        if (entry is null)
        {
            entry = new DailyHabitEntry
            {
                HabitId = habitId,
                Date = date,
                Value = value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.DailyHabitEntries.Add(entry);
        }
        else
        {
            entry.SetValue(value);
        }

        await db.SaveChangesAsync(ct);

        return new DailyEntryDto(habit.Id, habit.Name, habit.Unit, date, value, habit.DailyTarget);
    }

    public async Task<IReadOnlyList<DailyEntryDto>> GetHistoryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var habits = await db.Habits
            .Where(h => h.IsActive)
            .OrderBy(h => h.Id)
            .ToListAsync(ct);

        var entries = await db.DailyHabitEntries
            .Where(e => e.Date >= from && e.Date <= to)
            .ToListAsync(ct);

        var entryDict = entries.ToDictionary(e => (e.HabitId, e.Date), e => e.Value);

        var result = new List<DailyEntryDto>();
        foreach (var habit in habits)
        {
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                result.Add(new DailyEntryDto(
                    habit.Id,
                    habit.Name,
                    habit.Unit,
                    date,
                    entryDict.GetValueOrDefault((habit.Id, date), 0m),
                    habit.DailyTarget
                ));
            }
        }

        return result;
    }
}
