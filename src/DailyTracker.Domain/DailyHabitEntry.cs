namespace DailyTracker.Domain;

public class DailyHabitEntry
{
    public int Id { get; set; }
    public int HabitId { get; set; }
    public required DateTime Date { get; set; }
    public decimal Value { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Habit? Habit { get; set; }

    public void SetValue(decimal value)
    {
        if (value < 0)
            throw new ArgumentException("Value cannot be negative.", nameof(value));

        Value = value;
        UpdatedAt = DateTime.UtcNow;
    }
}
