# Daily Tracker -- Backend

ASP.NET Core Web API for tracking daily habits, built with .NET 10 and EF Core.

## Architecture

The backend uses a layered architecture:

```
src/
  DailyTracker.Domain/         # Entities (Habit, DailyHabitEntry)
  DailyTracker.Application/    # DTOs, service interfaces
  DailyTracker.Infrastructure/ # EF Core, SQLite, repositories, seed data
  DailyTracker.Api/            # Controllers, middleware, configuration
tests/
  DailyTracker.Tests/          # xUnit unit tests
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Getting Started

```bash
dotnet restore
dotnet run --project src/DailyTracker.Api
```

The API runs at `http://localhost:5000`.

## Build and Test

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run in development mode
dotnet run --project src/DailyTracker.Api --environment Development
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/habits` | List all active habits |
| `PATCH` | `/api/habits/{id}` | Update a habit's daily target |
| `GET` | `/api/daily-entries?date=YYYY-MM-DD` | Get entries for a date (zero-filled) |
| `PUT` | `/api/daily-entries/{habitId}/{date}` | Create or update a daily entry |
| `GET` | `/api/daily-entries/history?from=YYYY-MM-DD&to=YYYY-MM-DD` | Get history for a date range |
| `GET` | `/health` | Health check with database status |

## Database

Uses **SQLite** by default. The connection string is read from configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/data/dailytracker.db"
  }
}
```

### Migrations

```bash
# Add a migration
dotnet ef migrations add <Name> --project src/DailyTracker.Infrastructure --startup-project src/DailyTracker.Api

# Apply migrations
dotnet ef database update --project src/DailyTracker.Infrastructure --startup-project src/DailyTracker.Api
```

### Seed Data

On startup, the application seeds six default habits if the database is empty:
- Sleep (8h), Food (3 meals), Water (8 glasses), Walk (10,000 steps), Sun (10 min), Stand up (4 breaks)

## Environment Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| `PORT` | HTTP port to bind | `10000` |
| `ConnectionStrings__DefaultConnection` | SQLite file path | `/data/dailytracker.db` |
| `Cors__Origins__0` | Allowed CORS origin | `http://localhost:4200` |
| `Cors__Origins__1` | Allowed CORS origin | `https://box-frontend-1l88.vercel.app` |

## Docker

### Build

```bash
docker build -t daily-tracker-api .
```

### Run

```bash
docker run -p 10000:10000 \
  -e PORT=10000 \
  -e ConnectionStrings__DefaultConnection="Data Source=/data/dailytracker.db" \
  -v daily-tracker-data:/data \
  daily-tracker-api
```

The Dockerfile uses a multi-stage build:
1. **Build stage** -- .NET 10 SDK, restores and publishes in Release mode
2. **Runtime stage** -- .NET 10 ASP.NET runtime, copies published output

## Render Deployment

1. Create a **Web Service** in Render.
2. Set **Build Command** to `docker build -t daily-tracker-api .`
3. Set **Start Command** to `docker run -p $PORT:$PORT daily-tracker-api`
4. Add environment variables from the table above.
5. Render automatically injects the `PORT` variable.

## Health Checks

The `/health` endpoint returns a JSON response:

```json
{
  "status": "Healthy",
  "checks": [
    { "name": "database", "status": "Healthy", "description": null }
  ]
}
```

## Development Workflow

All changes follow a mandatory issue-first workflow: create an issue, branch from
`develop`, implement, validate, open a pull request targeting `develop`, review, and
release separately through a `develop` -> `main` pull request.

See [`docs/DEVELOPMENT_WORKFLOW.md`](docs/DEVELOPMENT_WORKFLOW.md) for the complete
process, including the solo-maintainer self-review policy.
