# Workit

Workit is a .NET 9 REST API starter using Clean Architecture, vertical slices, CQRS-style request handling, PostgreSQL, optional Redis, Hangfire, JWT authentication, Serilog, and Swagger.

## Prerequisites

- .NET 9 SDK
- Docker Desktop or Docker Engine
- A C# editor such as Cursor, VS Code, Rider, or Visual Studio
- Local tools restored with `dotnet tool restore`

## Environment Setup

VS Code and Cursor can load local values from `.env`. Rider can use `Workit.Api/Properties/launchSettings.json`; keep both files uncommitted because they are local settings.

The default local values are:

- API: `http://localhost:5187`
- Swagger: `http://localhost:5187/swagger`
- PostgreSQL host port: `5543`
- Redis host port: `6381`
- Database: `workit_dev`

## Docker Services

Start PostgreSQL and Redis:

```bash
docker compose up -d
```

The host connection string is:

```text
Host=localhost;Port=5543;Database=workit_dev;Username=postgres;Password=root
```

## Restore And Run

```bash
dotnet tool restore
dotnet restore
dotnet ef database update --project Workit.Core --startup-project Workit.Api --context AppDbContext
dotnet run --project Workit.Api
```

Open Swagger at `http://localhost:5187/swagger`.

## Auth Endpoints

Register:

```http
POST /auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

Login:

```http
POST /auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

Both endpoints return the user, JWT access token, and expiration timestamp.

## Tests

```bash
dotnet test
```

## Migrations

```bash
dotnet ef migrations add InitialCreate \
  --project Workit.Core \
  --startup-project Workit.Api \
  --context AppDbContext \
  --output-dir Shared/Persistence/Migrations

dotnet ef database update \
  --project Workit.Core \
  --startup-project Workit.Api \
  --context AppDbContext
```

## Troubleshooting

- Confirm Docker is running before starting PostgreSQL or Redis.
- Confirm ports `5187`, `5543`, `6381`, and `5342` are available.
- Confirm `.env` or `launchSettings.json` uses `Database=workit_dev`.
- If package restore fails, verify NuGet access to `https://api.nuget.org/v3/index.json`.
