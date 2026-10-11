namespace DailyTracker.Application.Dtos;

public record DailyEntryDto(int HabitId, string HabitName, string Unit, DateTime Date, decimal Value, decimal DailyTarget);
