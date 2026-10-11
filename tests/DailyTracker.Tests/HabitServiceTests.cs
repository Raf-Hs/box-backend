using DailyTracker.Application.Dtos;
using DailyTracker.Domain;
using DailyTracker.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DailyTracker.Tests;

public class HabitServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly HabitService _service;

    public HabitServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        _db = new AppDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _service = new HabitService(_db);
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }

    [Fact]
    public async Task GetActiveHabitsAsync_ReturnsSeededHabits()
    {
        await SeedData.SeedHabitsAsync(_db);
        var habits = await _service.GetActiveHabitsAsync();
        Assert.Equal(6, habits.Count);
    }

    [Fact]
    public async Task UpdateTargetAsync_UpdatesTarget()
    {
        await SeedData.SeedHabitsAsync(_db);
        var habit = await _service.UpdateTargetAsync(1, 10m);
        Assert.Equal(10m, habit.DailyTarget);
    }

    [Fact]
    public async Task UpdateTargetAsync_NegativeTarget_Throws()
    {
        await SeedData.SeedHabitsAsync(_db);
        await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateTargetAsync(1, -1m));
    }

    [Fact]
    public async Task UpdateTargetAsync_InvalidHabit_Throws()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateTargetAsync(999, 5m));
    }
}
