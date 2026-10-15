using System.Net;
using System.Net.Http.Json;
using static RaceDay.Tests.TestHelpers;

namespace RaceDay.Tests;

public class EventTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Organiser_CanCreateEvent()
    {
        var org = await factory.LoginAs("Organiser");
        var r = await org.PostAsJsonAsync("/api/events", EventBody());
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_CreateEvent_InvalidData_ReturnsBadRequest()
    {
        var org = await factory.LoginAs("Organiser");
        var r = await org.PostAsJsonAsync("/api/events", new { name = "", description = "x", date = DateTime.UtcNow, location = "x", distance = 5, eventType = "Run" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var org = await factory.LoginAs("Organiser");
        var id = await CreateEvent(org);
        var r = await org.PutAsJsonAsync($"/api/events/{id}", EventBody("Renamed Race"));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("Renamed Race", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent()
    {
        var org = await factory.LoginAs("Organiser");
        var id = await CreateEvent(org);
        Assert.Equal(HttpStatusCode.NoContent, (await org.DeleteAsync($"/api/events/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await org.GetAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent()
    {
        var owner = await factory.LoginAs("Organiser");
        var other = await factory.LoginAs("Organiser");
        var id = await CreateEvent(owner);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync($"/api/events/{id}", EventBody("Hijack"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateUpdateOrDeleteEvents()
    {
        var org = await factory.LoginAs("Organiser");
        var part = await factory.LoginAs("Participant");
        var id = await CreateEvent(org);
        Assert.Equal(HttpStatusCode.Forbidden, (await part.PostAsJsonAsync("/api/events", EventBody())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await part.PutAsJsonAsync($"/api/events/{id}", EventBody())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await part.DeleteAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewEvents()
    {
        var org = await factory.LoginAs("Organiser");
        var part = await factory.LoginAs("Participant");
        await CreateEvent(org);
        Assert.Equal(HttpStatusCode.OK, (await part.GetAsync("/api/events")).StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_CannotViewOrCreateEvents()
    {
        var anon = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/events")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsJsonAsync("/api/events", EventBody())).StatusCode);
    }

    [Fact]
    public async Task Organiser_CanManageCategories_ParticipantCannot()
    {
        var org = await factory.LoginAs("Organiser");
        var part = await factory.LoginAs("Participant");
        var eventId = await CreateEvent(org);

        Assert.Equal(HttpStatusCode.Created, (await org.PostAsJsonAsync($"/api/events/{eventId}/categories", new { name = "Under 20", maxAge = 19 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await part.PostAsJsonAsync($"/api/events/{eventId}/categories", new { name = "Sneaky" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await part.GetAsync($"/api/events/{eventId}/categories")).StatusCode);
    }

    [Fact]
    public async Task Category_MinAgeGreaterThanMaxAge_ReturnsBadRequest()
    {
        var org = await factory.LoginAs("Organiser");
        var eventId = await CreateEvent(org);
        var r = await org.PostAsJsonAsync($"/api/events/{eventId}/categories", new { name = "Bad", minAge = 50, maxAge = 20 });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }
}
