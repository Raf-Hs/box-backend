using DailyTracker.Application;
using DailyTracker.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DailyTracker.Api.Controllers;

[ApiController]
[Route("api/daily-entries")]
public class DailyEntriesController(IDailyEntryService entryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DailyEntryDto>>> GetEntries(
        [FromQuery] DateTime date, CancellationToken ct)
    {
        var entries = await entryService.GetEntriesForDateAsync(date, ct);
        return Ok(entries);
    }

    [HttpPut("{habitId:int}/{date:datetime}")]
    public async Task<ActionResult<DailyEntryDto>> SetEntryValue(
        int habitId, DateTime date, [FromBody] SetValueRequest request, CancellationToken ct)
    {
        try
        {
            var entry = await entryService.SetEntryValueAsync(habitId, date, request.Value, ct);
            return Ok(entry);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Message = $"Habit {habitId} not found." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<DailyEntryDto>>> GetHistory(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { Message = "'from' must be before or equal to 'to'." });

        var entries = await entryService.GetHistoryAsync(from, to, ct);
        return Ok(entries);
    }
}

public record SetValueRequest(decimal Value);
