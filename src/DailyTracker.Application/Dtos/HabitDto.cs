namespace DailyTracker.Application.Dtos;

public record HabitDto(int Id, string Name, string Unit, decimal DailyTarget, bool IsActive);
