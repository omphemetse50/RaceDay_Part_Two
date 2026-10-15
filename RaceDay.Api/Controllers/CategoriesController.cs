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
public class CategoriesController(AppDbContext db) : ControllerBase
{
    /// <summary>Lists the categories of an event (any logged-in user).</summary>
    /// <response code="200">The event's categories.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.Id == eventId)) return NotFound();
        var cats = await db.Categories.AsNoTracking().Where(c => c.EventId == eventId).OrderBy(c => c.Name).ToListAsync();
        return Ok(cats.Select(Mapper.Map).ToList());
    }

    /// <summary>Adds an age or distance category to an event (the event's Organiser).</summary>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation failed (e.g. MinAge greater than MaxAge).</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost("events/{eventId:int}/categories"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, CategoryInputDto dto)
    {
        var ev = await db.Events.FindAsync(eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != User.UserId()) return Forbid();
        if (dto.MinAge > dto.MaxAge) return BadRequest(new { message = "MinAge cannot be greater than MaxAge." });

        var c = new Category { EventId = eventId, Name = dto.Name, MinAge = dto.MinAge, MaxAge = dto.MaxAge, DistanceKm = dto.DistanceKm };
        db.Categories.Add(c);
        await db.SaveChangesAsync();
        return Created($"/api/events/{eventId}/categories", Mapper.Map(c));
    }

    /// <summary>Updates a category (the event's Organiser).</summary>
    /// <response code="200">Updated category.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("categories/{id:int}"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CategoryInputDto dto)
    {
        var c = await db.Categories.Include(x => x.Event).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        if (c.Event.OrganiserId != User.UserId()) return Forbid();
        if (dto.MinAge > dto.MaxAge) return BadRequest(new { message = "MinAge cannot be greater than MaxAge." });
        c.Name = dto.Name; c.MinAge = dto.MinAge; c.MaxAge = dto.MaxAge; c.DistanceKm = dto.DistanceKm;
        await db.SaveChangesAsync();
        return Ok(Mapper.Map(c));
    }

    /// <summary>Deletes a category that has no enrolments (the event's Organiser).</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Category already has enrolments.</response>
    [HttpDelete("categories/{id:int}"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Categories.Include(x => x.Event).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        if (c.Event.OrganiserId != User.UserId()) return Forbid();
        if (await db.Enrolments.AnyAsync(e => e.CategoryId == id))
            return Conflict(new { message = "Category has enrolments and cannot be deleted." });
        db.Categories.Remove(c);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
