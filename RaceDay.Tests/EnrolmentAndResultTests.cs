using System.Net;
using System.Net.Http.Json;
using static RaceDay.Tests.TestHelpers;

namespace RaceDay.Tests;

public class EnrolmentAndResultTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<(HttpClient org, int eventId, int categoryId)> SeedEvent()
    {
        var org = await factory.LoginAs("Organiser");
        var eventId = await CreateEvent(org);
        var catId = await CreateCategory(org, eventId);
        return (org, eventId, catId);
    }

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecorded()
    {
        var (_, eventId, catId) = await SeedEvent();
        var part = await factory.LoginAs("Participant");

        var r = await part.PostAsJsonAsync("/api/enrolments", new { categoryId = catId });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);

        var mine = await part.GetStringAsync("/api/enrolments/mine");
        Assert.Contains($"\"eventId\":{eventId}", mine);
        Assert.Contains($"\"categoryId\":{catId}", mine);
    }

    [Fact]
    public async Task Participant_CannotEnrolTwiceInSameEvent()
    {
        var (_, _, catId) = await SeedEvent();
        var part = await factory.LoginAs("Participant");
        await part.PostAsJsonAsync("/api/enrolments", new { categoryId = catId });
        var r = await part.PostAsJsonAsync("/api/enrolments", new { categoryId = catId });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Enrol_UnknownCategory_ReturnsNotFound()
    {
        var part = await factory.LoginAs("Participant");
        var r = await part.PostAsJsonAsync("/api/enrolments", new { categoryId = 99999 });
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrol()
    {
        var (org, _, catId) = await SeedEvent();
        var r = await org.PostAsJsonAsync("/api/enrolments", new { categoryId = catId });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_CannotEnrol()
    {
        var (_, _, catId) = await SeedEvent();
        var r = await factory.CreateClient().PostAsJsonAsync("/api/enrolments", new { categoryId = catId });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForOwnEvent_ParticipantCannot()
    {
        var (org, eventId, catId) = await SeedEvent();
        var part = await factory.LoginAs("Participant");
        await part.PostAsJsonAsync("/api/enrolments", new { categoryId = catId });

        var ok = await org.GetAsync($"/api/events/{eventId}/enrolments");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Contains($"\"categoryId\":{catId}", await ok.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Forbidden, (await part.GetAsync($"/api/events/{eventId}/enrolments")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCaptureResult_ParticipantCanViewOwn()
    {
        var (org, _, catId) = await SeedEvent();
        var part = await factory.LoginAs("Participant");
        var enrolmentId = await IdOf(await part.PostAsJsonAsync("/api/enrolments", new { categoryId = catId }));

        var created = await org.PostAsJsonAsync("/api/results", new { enrolmentId, finishTime = "00:45:30", position = 1 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var mine = await part.GetAsync("/api/results/mine");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        Assert.Contains("00:45:30", await mine.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Participant_CannotCaptureResults()
    {
        var (_, _, catId) = await SeedEvent();
        var part = await factory.LoginAs("Participant");
        var enrolmentId = await IdOf(await part.PostAsJsonAsync("/api/enrolments", new { categoryId = catId }));
        var r = await part.PostAsJsonAsync("/api/results", new { enrolmentId, finishTime = "00:30:00", position = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCaptureResultForAnotherOrganisersEvent()
    {
        var (_, _, catId) = await SeedEvent();
        var part = await factory.LoginAs("Participant");
        var enrolmentId = await IdOf(await part.PostAsJsonAsync("/api/enrolments", new { categoryId = catId }));
        var otherOrg = await factory.LoginAs("Organiser");
        var r = await otherOrg.PostAsJsonAsync("/api/results", new { enrolmentId, finishTime = "00:30:00", position = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Result_DuplicateForSameEnrolment_ReturnsConflict()
    {
        var (org, _, catId) = await SeedEvent();
        var part = await factory.LoginAs("Participant");
        var enrolmentId = await IdOf(await part.PostAsJsonAsync("/api/enrolments", new { categoryId = catId }));
        await org.PostAsJsonAsync("/api/results", new { enrolmentId, finishTime = "00:40:00", position = 1 });
        var r = await org.PostAsJsonAsync("/api/results", new { enrolmentId, finishTime = "00:41:00", position = 2 });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Result_InvalidFinishTime_ReturnsBadRequest()
    {
        var (org, _, catId) = await SeedEvent();
        var part = await factory.LoginAs("Participant");
        var enrolmentId = await IdOf(await part.PostAsJsonAsync("/api/enrolments", new { categoryId = catId }));
        var r = await org.PostAsJsonAsync("/api/results", new { enrolmentId, finishTime = "00:00:00", position = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }
}
