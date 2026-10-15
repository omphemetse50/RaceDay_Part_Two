using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventsController(AppDbContext db) : ControllerBase
{
    /// <summary>Lists all events (any logged-in user).</summary>
    /// <response code="200">All events ordered by date.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var events = await db.Events.AsNoTracking().OrderBy(e => e.Date).ToListAsync();
        return Ok(events.Select(Mapper.Map).ToList());
    }

    /// <summary>Gets one event by id (any logged-in user).</summary>
    /// <response code="200">The event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var e = await db.Events.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return e == null ? NotFound() : Ok(Mapper.Map(e));
    }

    /// <summary>Creates an event (Organisers only).</summary>
    /// <remarks>Needs name, description, date, location, distance (km) and event type (Run, Walk or Cycle).</remarks>
    /// <response code="201">Event created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    [HttpPost, Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventInputDto dto)
    {
        var e = new Event
        {
            Name = dto.Name, Description = dto.Description, Date = dto.Date, Location = dto.Location,
            Distance = dto.Distance, EventType = dto.EventType, OrganiserId = User.UserId()
        };
        db.Events.Add(e);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = e.Id }, Mapper.Map(e));
    }

    /// <summary>Updates an event (the Organiser who created it).</summary>
    /// <response code="200">Updated event.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{id:int}"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventInputDto dto)
    {
        var e = await db.Events.FindAsync(id);
        if (e == null) return NotFound();
        if (e.OrganiserId != User.UserId()) return Forbid();
        e.Name = dto.Name; e.Description = dto.Description; e.Date = dto.Date;
        e.Location = dto.Location; e.Distance = dto.Distance; e.EventType = dto.EventType;
        await db.SaveChangesAsync();
        return Ok(Mapper.Map(e));
    }

    /// <summary>Deletes an event and its categories and enrolments (the Organiser who created it).</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpDelete("{id:int}"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await db.Events.FindAsync(id);
        if (e == null) return NotFound();
        if (e.OrganiserId != User.UserId()) return Forbid();
        db.Events.Remove(e);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
