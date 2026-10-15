using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class RegisterDto
{
    [Required, StringLength(50)] public string FirstName { get; set; } = "";
    [Required, StringLength(50)] public string LastName { get; set; } = "";
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = "";
    [Required, EnumDataType(typeof(UserRole))] public UserRole Role { get; set; }
    [StringLength(30)] public string? Phone { get; set; }
}

public class LoginDto
{
    [Required] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class UpdateProfileDto
{
    [Required, StringLength(50)] public string FirstName { get; set; } = "";
    [Required, StringLength(50)] public string LastName { get; set; } = "";
    [StringLength(30)] public string? Phone { get; set; }
}

public class EventInputDto
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, StringLength(1000)] public string Description { get; set; } = "";
    [Required] public DateTime Date { get; set; }
    [Required, StringLength(150)] public string Location { get; set; } = "";
    [Range(0.1, 1000)] public decimal Distance { get; set; }
    [Required, EnumDataType(typeof(EventType))] public EventType EventType { get; set; }
}

public class CategoryInputDto
{
    [Required, StringLength(60)] public string Name { get; set; } = "";
    [Range(0, 120)] public int? MinAge { get; set; }
    [Range(0, 120)] public int? MaxAge { get; set; }
    [Range(0.1, 1000)] public decimal? DistanceKm { get; set; }
}

public class EnrolDto
{
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
}

public class CreateResultDto
{
    [Range(1, int.MaxValue)] public int EnrolmentId { get; set; }
    /// <summary>Finish time as hh:mm:ss (under 24 hours).</summary>
    public TimeSpan FinishTime { get; set; }
    [Range(1, 100000)] public int Position { get; set; }
}

public class UpdateResultDto
{
    /// <summary>Finish time as hh:mm:ss (under 24 hours).</summary>
    public TimeSpan FinishTime { get; set; }
    [Range(1, 100000)] public int Position { get; set; }
}

public record ProfileDto(int Id, string FirstName, string LastName, string Email, string? Phone, UserRole Role);
public record EventDto(int Id, string Name, string Description, DateTime Date, string Location, decimal Distance, EventType EventType, int OrganiserId);
public record CategoryDto(int Id, int EventId, string Name, int? MinAge, int? MaxAge, decimal? DistanceKm);
public record EnrolmentDto(int Id, int EventId, string EventName, int CategoryId, string CategoryName, int ParticipantId, string ParticipantName, DateTime EnrolledAt);
public record ResultDto(int Id, int EnrolmentId, int EventId, string EventName, string CategoryName, string ParticipantName, TimeSpan FinishTime, int Position);

public static class Mapper
{
    public static ProfileDto Map(User u) => new(u.Id, u.FirstName, u.LastName, u.Email, u.Phone, u.Role);
    public static EventDto Map(Event e) => new(e.Id, e.Name, e.Description, e.Date, e.Location, e.Distance, e.EventType, e.OrganiserId);
    public static CategoryDto Map(Category c) => new(c.Id, c.EventId, c.Name, c.MinAge, c.MaxAge, c.DistanceKm);
    public static EnrolmentDto Map(Enrolment e) => new(e.Id, e.EventId, e.Event.Name, e.CategoryId, e.Category.Name,
        e.ParticipantId, $"{e.Participant.FirstName} {e.Participant.LastName}", e.EnrolledAt);
    public static ResultDto Map(Result r) => new(r.Id, r.EnrolmentId, r.Enrolment.EventId, r.Enrolment.Event.Name,
        r.Enrolment.Category.Name, $"{r.Enrolment.Participant.FirstName} {r.Enrolment.Participant.LastName}", r.FinishTime, r.Position);
}
