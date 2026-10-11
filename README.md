# Daily Tracker

A personal daily habit-tracking application built with **Angular 19** (frontend) and **.NET 10** (backend).

## Repository Layout

| Directory | Description |
|-----------|-------------|
| `frontend/` | Angular standalone app — dashboard, history, habit configuration |
| `backend/` | ASP.NET Core Web API — layered architecture (Domain, Application, Infrastructure, Api) |

## Backend (.NET 10)

### Local Development

```bash
cd backend
dotnet restore
dotnet ef database update --project src/DailyTracker.Infrastructure --startup-project src/DailyTracker.Api
dotnet run --project src/DailyTracker.Api
```

The API will be available at `http://localhost:5000`.

### Running Tests

```bash
cd backend
dotnet test
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/habits` | Get all active habits |
| `PATCH` | `/api/habits/{id}` | Update a habit's daily target |
| `GET` | `/api/daily-entries?date=YYYY-MM-DD` | Get entries for a date |
| `PUT` | `/api/daily-entries/{habitId}/{date}` | Create or update an entry |
| `GET` | `/api/daily-entries/history?from=YYYY-MM-DD&to=YYYY-MM-DD` | Get history range |
| `GET` | `/health` | Health check |

### Docker Deployment (Render)

The backend includes a multi-stage `Dockerfile` ready for Render.

**Required environment variables:**

| Variable | Description |
|----------|-------------|
| `PORT` | Port to bind (Render sets this automatically) |
| `ConnectionStrings__DefaultConnection` | SQLite connection string (defaults to `/data/dailytracker.db`) |

**Optional environment variables:**

| Variable | Description |
|----------|-------------|
| `Cors__Origins__0` | Allowed CORS origin (defaults to `http://localhost:4200`) |

**Build and run locally with Docker:**

```bash
cd backend
docker build -t daily-tracker-api .
docker run -p 10000:10000 -e PORT=10000 daily-tracker-api
```

### Database

Uses **SQLite** by default for local development. The connection string can be switched to SQL Server by changing the provider in `Program.cs` and updating the connection string.

Seed data populates six default habits on first run (Sleep, Food, Water, Walk, Sun, Stand up).

## Frontend (Angular 19)

### Local Development

```bash
cd frontend
npm install
ng serve
```

The app will be available at `http://localhost:4200`.

### Build for Production

```bash
cd frontend
npm run build
```

## Technology Stack

- **Frontend:** Angular 19, TypeScript, standalone components, Angular Router, HttpClient, Reactive Forms
- **Backend:** .NET 10, ASP.NET Core Web API, EF Core 10, SQLite
- **Testing:** xUnit
