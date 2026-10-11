namespace DailyTracker.Domain;

public class Habit
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Unit { get; set; }
    public decimal DailyTarget { get; set; }
    public bool IsActive { get; set; } = true;

    public void UpdateTarget(decimal newTarget)
    {
        if (newTarget < 0)
            throw new ArgumentException("Target cannot be negative.", nameof(newTarget));

        DailyTarget = newTarget;
    }
}
