using DailyTracker.Application.Dtos;

namespace DailyTracker.Application;

public interface IDailyEntryService
{
    Task<IReadOnlyList<DailyEntryDto>> GetEntriesForDateAsync(DateTime date, CancellationToken ct = default);
    Task<DailyEntryDto> SetEntryValueAsync(int habitId, DateTime date, decimal value, CancellationToken ct = default);
    Task<IReadOnlyList<DailyEntryDto>> GetHistoryAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
