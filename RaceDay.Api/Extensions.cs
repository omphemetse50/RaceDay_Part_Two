using System.Security.Claims;

namespace RaceDay.Api;

public static class ClaimsExtensions
{
    /// <summary>Reads the logged-in user's id from the session cookie's claims.</summary>
    public static int UserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
