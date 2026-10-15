using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class ResultsController(AppDbContext db) : ControllerBase
{
    private IQueryable<Result> Query() => db.Results
        .Include(r => r.Enrolment).ThenInclude(e => e.Event)
        .Include(r => r.Enrolment).ThenInclude(e => e.Category)
        .Include(r => r.Enrolment).ThenInclude(e => e.Participant);

    private static bool ValidTime(TimeSpan t) => t > TimeSpan.Zero && t < TimeSpan.FromHours(24);

    /// <summary>Captures a Participant's finish time and position (the event's Organiser).</summary>
    /// <remarks>Finish time is sent as hh:mm:ss. Each enrolment can have one result.</remarks>
    /// <response code="201">Result recorded.</response>
    /// <response code="400">Invalid finish time or validation failed.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result already exists for this enrolment.</response>
    [HttpPost("results"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateResultDto dto)
    {
        if (!ValidTime(dto.FinishTime)) return BadRequest(new { message = "FinishTime must be between 00:00:01 and 23:59:59." });
        var en = await db.Enrolments.Include(e => e.Event).FirstOrDefaultAsync(e => e.Id == dto.EnrolmentId);
        if (en == null) return NotFound(new { message = "Enrolment not found." });
        if (en.Event.OrganiserId != User.UserId()) return Forbid();
        if (await db.Results.AnyAsync(r => r.EnrolmentId == en.Id))
            return Conflict(new { message = "A result already exists for this enrolment." });

        var r = new Result { EnrolmentId = en.Id, FinishTime = dto.FinishTime, Position = dto.Position, RecordedAt = DateTime.UtcNow };
        db.Results.Add(r);
        await db.SaveChangesAsync();
        var saved = await Query().AsNoTracking().FirstAsync(x => x.Id == r.Id);
        return Created($"/api/events/{en.EventId}/results", Mapper.Map(saved));
    }

    /// <summary>Corrects a captured result (the event's Organiser).</summary>
    /// <response code="200">Updated result.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Result not found.</response>
    [HttpPut("results/{id:int}"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateResultDto dto)
    {
        if (!ValidTime(dto.FinishTime)) return BadRequest(new { message = "FinishTime must be between 00:00:01 and 23:59:59." });
        var r = await Query().FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        if (r.Enrolment.Event.OrganiserId != User.UserId()) return Forbid();
        r.FinishTime = dto.FinishTime;
        r.Position = dto.Position;
        await db.SaveChangesAsync();
        return Ok(Mapper.Map(r));
    }

    /// <summary>Lists all results for an event, ordered by position (the event's Organiser).</summary>
    /// <response code="200">Results for the event.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/results"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        var ev = await db.Events.FindAsync(eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != User.UserId()) return Forbid();
        var list = await Query().AsNoTracking().Where(r => r.Enrolment.EventId == eventId).OrderBy(r => r.Position).ToListAsync();
        return Ok(list.Select(Mapper.Map).ToList());
    }

    /// <summary>Lists the logged-in Participant's own results.</summary>
    /// <response code="200">The Participant's results.</response>
    /// <response code="403">Caller is not a Participant.</response>
    [HttpGet("results/mine"), Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Mine()
    {
        var uid = User.UserId();
        var list = await Query().AsNoTracking().Where(r => r.Enrolment.ParticipantId == uid).ToListAsync();
        return Ok(list.Select(Mapper.Map).ToList());
    }
}
