using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

/// <summary>Runs the real API in memory, swapping SQL Server for an isolated in-memory database.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}

public static class TestHelpers
{
    public const string Password = "Passw0rd!";

    public static object NewUser(string email, string role) =>
        new { firstName = "Test", lastName = "User", email, password = Password, role };

    public static string NewEmail() => $"{Guid.NewGuid():N}@test.com";

    /// <summary>Registers a fresh user, logs in, and returns a client holding that user's session cookie.</summary>
    public static async Task<HttpClient> LoginAs(this ApiFactory factory, string role)
    {
        var client = factory.CreateClient();
        var email = NewEmail();
        (await client.PostAsJsonAsync("/api/auth/register", NewUser(email, role))).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password })).EnsureSuccessStatusCode();
        return client;
    }

    public static async Task<int> IdOf(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetInt32();
    }

    public static object EventBody(string name = "City 10K") => new
    {
        name, description = "A fun city race", date = DateTime.UtcNow.AddDays(30),
        location = "Johannesburg", distance = 10m, eventType = "Run"
    };

    public static async Task<int> CreateEvent(HttpClient organiser, string name = "City 10K") =>
        await IdOf(await organiser.PostAsJsonAsync("/api/events", EventBody(name)));

    public static async Task<int> CreateCategory(HttpClient organiser, int eventId, string name = "Senior") =>
        await IdOf(await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new { name, minAge = 35, maxAge = 120 }));
}
