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
public class EnrolmentsController(AppDbContext db) : ControllerBase
{
    private IQueryable<Enrolment> Query() => db.Enrolments
        .Include(e => e.Event).Include(e => e.Category).Include(e => e.Participant);

    /// <summary>Enrols the logged-in Participant in an event by choosing a category.</summary>
    /// <remarks>The enrolment links the Participant, the event and the chosen category. A Participant can enrol once per event.</remarks>
    /// <response code="201">Enrolment recorded.</response>
    /// <response code="403">Caller is not a Participant.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Already enrolled in this event.</response>
    [HttpPost("enrolments"), Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(EnrolDto dto)
    {
        var cat = await db.Categories.FindAsync(dto.CategoryId);
        if (cat == null) return NotFound(new { message = "Category not found." });

        var uid = User.UserId();
        if (await db.Enrolments.AnyAsync(e => e.EventId == cat.EventId && e.ParticipantId == uid))
            return Conflict(new { message = "You are already enrolled in this event." });

        var en = new Enrolment { EventId = cat.EventId, CategoryId = cat.Id, ParticipantId = uid, EnrolledAt = DateTime.UtcNow };
        db.Enrolments.Add(en);
        await db.SaveChangesAsync();

        var saved = await Query().AsNoTracking().FirstAsync(e => e.Id == en.Id);
        return Created("/api/enrolments/mine", Mapper.Map(saved));
    }

    /// <summary>Lists the logged-in Participant's own enrolments.</summary>
    /// <response code="200">The Participant's enrolments.</response>
    /// <response code="403">Caller is not a Participant.</response>
    [HttpGet("enrolments/mine"), Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Mine()
    {
        var uid = User.UserId();
        var list = await Query().AsNoTracking().Where(e => e.ParticipantId == uid).ToListAsync();
        return Ok(list.Select(Mapper.Map).ToList());
    }

    /// <summary>Lists all enrolments for an event (the event's Organiser).</summary>
    /// <response code="200">Enrolments for the event.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/enrolments"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        var ev = await db.Events.FindAsync(eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != User.UserId()) return Forbid();
        var list = await Query().AsNoTracking().Where(e => e.EventId == eventId).ToListAsync();
        return Ok(list.Select(Mapper.Map).ToList());
    }

    /// <summary>Cancels one of the Participant's own enrolments.</summary>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="403">Caller is not a Participant, or the enrolment is not theirs.</response>
    /// <response code="404">Enrolment not found.</response>
    [HttpDelete("enrolments/{id:int}"), Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
    {
        var en = await db.Enrolments.FindAsync(id);
        if (en == null) return NotFound();
        if (en.ParticipantId != User.UserId()) return Forbid();
        db.Enrolments.Remove(en);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
