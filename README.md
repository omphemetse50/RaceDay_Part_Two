# RaceDay API (Part 2)

RESTful ASP.NET Core 8 Web API for the RaceDay event system. Organisers create events and categories and capture results. Participants enrol in events and view their own results.

![CI](https://github.com/<your-username>/<your-repo>/actions/workflows/ci.yml/badge.svg)

## Tech
ASP.NET Core 8 Web API, Entity Framework Core 8 (Code First), SQL Server (LocalDB by default), cookie-based session authentication, BCrypt password hashing, Swagger (Swashbuckle), xUnit, GitHub Actions.

## Setup and run
1. Install the .NET 8 SDK and SQL Server (or SQL Server LocalDB, which ships with Visual Studio).
2. Check the connection string in `RaceDay.Api/appsettings.json`.
3. Create the first migration (once):
   ```
   dotnet tool install --global dotnet-ef
   dotnet ef migrations add InitialCreate --project RaceDay.Api
   ```
4. Run the API (the database is created and migrated automatically on startup):
   ```
   dotnet run --project RaceDay.Api
   ```
5. Open `/swagger` in your browser (the port is shown in the console).
6. Run tests: `dotnet test RaceDay.Tests`

## Using Swagger
Call `POST /api/auth/register`, then `POST /api/auth/login`. The browser stores the session cookie, so every other endpoint can then be tried directly from the Swagger UI. Use `POST /api/auth/logout` to end the session.

## Roles
| Role | Can do |
|------|--------|
| **Organiser** | Create, update and delete their own events; define categories; view all enrolments for their events; capture and correct results. |
| **Participant** | View events and categories; enrol in an event by choosing a category; view and cancel their own enrolments; view their own results. |

Both roles can register, log in and manage their own profile. The role is chosen at registration and stored in the session cookie at login. Role and ownership checks are enforced on the server: wrong role returns 403, not logged in returns 401.

## Endpoints
| Area | Endpoints |
|------|-----------|
| Auth | `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout` |
| Profile | `GET /api/profile`, `PUT /api/profile` |
| Events | `GET /api/events`, `GET /api/events/{id}`, `POST`, `PUT /{id}`, `DELETE /{id}` |
| Categories | `GET/POST /api/events/{eventId}/categories`, `PUT/DELETE /api/categories/{id}` |
| Enrolments | `POST /api/enrolments`, `GET /api/enrolments/mine`, `GET /api/events/{eventId}/enrolments`, `DELETE /api/enrolments/{id}` |
| Results | `POST /api/results`, `PUT /api/results/{id}`, `GET /api/events/{eventId}/results`, `GET /api/results/mine` |

## CI/CD
`.github/workflows/ci.yml` builds the solution and runs all unit tests on every push.

![Green build](docs/ci-green.png)

## Demo video
[YouTube (unlisted): add link here](https://youtube.com/)
