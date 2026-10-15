using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using static RaceDay.Tests.TestHelpers;

namespace RaceDay.Tests;

public class AuthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Register_ValidData_ReturnsCreated(string role)
    {
        var r = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", NewUser(NewEmail(), role));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/register", NewUser(email, "Participant"));
        var r = await client.PostAsJsonAsync("/api/auth/register", NewUser(email, "Participant"));
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_ReturnsBadRequest()
    {
        var body = new { firstName = "A", lastName = "B", email = NewEmail(), password = "short", role = "Participant" };
        var r = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", body);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_ReturnsBadRequest()
    {
        var r = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", NewUser(NewEmail(), "Admin"));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        var client = factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/register", NewUser(email, "Participant"));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.Single(u => u.Email == email);
        Assert.NotEqual(Password, user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash));
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOk()
    {
        var client = factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/register", NewUser(email, "Participant"));
        var r = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/register", NewUser(email, "Participant"));
        var r = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPass1!" });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownUser_ReturnsUnauthorized()
    {
        var r = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = NewEmail(), password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Profile_WithoutLogin_ReturnsUnauthorized()
    {
        var r = await factory.CreateClient().GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Profile_AfterLogin_ReturnsOwnProfile()
    {
        var client = await factory.LoginAs("Participant");
        var r = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Profile_Update_ChangesName()
    {
        var client = await factory.LoginAs("Participant");
        var r = await client.PutAsJsonAsync("/api/profile", new { firstName = "Changed", lastName = "Name", phone = "0821234567" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var text = await (await client.GetAsync("/api/profile")).Content.ReadAsStringAsync();
        Assert.Contains("Changed", text);
    }

    [Fact]
    public async Task Logout_EndsSession()
    {
        var client = await factory.LoginAs("Participant");
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/profile")).StatusCode);
    }
}
