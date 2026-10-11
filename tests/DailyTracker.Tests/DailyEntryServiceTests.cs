using DailyTracker.Application.Dtos;
using DailyTracker.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DailyTracker.Tests;

public class DailyEntryServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly DailyEntryService _service;

    public DailyEntryServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        _db = new AppDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _service = new DailyEntryService(_db);
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }

    [Fact]
    public async Task GetEntriesForDateAsync_NoEntries_ReturnsZeroValues()
    {
        await SeedData.SeedHabitsAsync(_db);
        var date = new DateTime(2025, 1, 15);
        var entries = await _service.GetEntriesForDateAsync(date);
        Assert.Equal(6, entries.Count);
        Assert.All(entries, e => Assert.Equal(0m, e.Value));
    }

    [Fact]
    public async Task SetEntryValueAsync_CreatesNewEntry()
    {
        await SeedData.SeedHabitsAsync(_db);
        var date = new DateTime(2025, 1, 15);
        var entry = await _service.SetEntryValueAsync(1, date, 5m);
        Assert.Equal(5m, entry.Value);
    }

    [Fact]
    public async Task SetEntryValueAsync_UpdatesExistingEntry()
    {
        await SeedData.SeedHabitsAsync(_db);
        var date = new DateTime(2025, 1, 15);
        await _service.SetEntryValueAsync(1, date, 5m);
        var updated = await _service.SetEntryValueAsync(1, date, 7m);
        Assert.Equal(7m, updated.Value);
    }

    [Fact]
    public async Task SetEntryValueAsync_NegativeValue_Throws()
    {
        await SeedData.SeedHabitsAsync(_db);
        var date = new DateTime(2025, 1, 15);
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SetEntryValueAsync(1, date, -1m));
    }

    [Fact]
    public async Task SetEntryValueAsync_InvalidHabit_Throws()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.SetEntryValueAsync(999, DateTime.Now, 5m));
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsEntriesInRange()
    {
        await SeedData.SeedHabitsAsync(_db);
        var date = new DateTime(2025, 1, 15);
        await _service.SetEntryValueAsync(1, date, 5m);
        await _service.SetEntryValueAsync(2, date.AddDays(1), 3m);

        var history = await _service.GetHistoryAsync(date, date.AddDays(1));
        Assert.Equal(12, history.Count); // 6 habits x 2 days
    }

    [Fact]
    public async Task GetEntriesForDateAsync_DifferentDates_PreservesData()
    {
        await SeedData.SeedHabitsAsync(_db);
        var date1 = new DateTime(2025, 1, 15);
        var date2 = new DateTime(2025, 1, 16);
        await _service.SetEntryValueAsync(1, date1, 5m);
        await _service.SetEntryValueAsync(1, date2, 8m);

        var entries1 = await _service.GetEntriesForDateAsync(date1);
        var entries2 = await _service.GetEntriesForDateAsync(date2);

        var sleep1 = entries1.First(e => e.HabitId == 1);
        var sleep2 = entries2.First(e => e.HabitId == 1);
        Assert.Equal(5m, sleep1.Value);
        Assert.Equal(8m, sleep2.Value);
    }
}
