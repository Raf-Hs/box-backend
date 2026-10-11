using DailyTracker.Application;
using DailyTracker.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DailyTracker.Api.Controllers;

[ApiController]
[Route("api/habits")]
public class HabitsController(IHabitService habitService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HabitDto>>> GetHabits(CancellationToken ct)
    {
        var habits = await habitService.GetActiveHabitsAsync(ct);
        return Ok(habits);
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<HabitDto>> UpdateTarget(int id, [FromBody] UpdateTargetRequest request, CancellationToken ct)
    {
        try
        {
            var habit = await habitService.UpdateTargetAsync(id, request.DailyTarget, ct);
            return Ok(habit);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Message = $"Habit {id} not found." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}

public record UpdateTargetRequest(decimal DailyTarget);
