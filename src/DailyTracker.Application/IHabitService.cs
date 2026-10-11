using DailyTracker.Application.Dtos;

namespace DailyTracker.Application;

public interface IHabitService
{
    Task<IReadOnlyList<HabitDto>> GetActiveHabitsAsync(CancellationToken ct = default);
    Task<HabitDto?> GetHabitAsync(int id, CancellationToken ct = default);
    Task<HabitDto> UpdateTargetAsync(int id, decimal newTarget, CancellationToken ct = default);
}
