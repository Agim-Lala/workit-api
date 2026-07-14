# Repository Guidelines

## Project Structure & Module Organization

Workit is a .NET 9 solution using Clean Architecture, vertical slices, and CQRS-style MediatR use cases.

- `Workit.Api/`: minimal API presentation layer, endpoint files, routing, Swagger, auth, Hangfire, and startup composition.
- `Workit.Core/`: domain models, use cases, validators, shared services, persistence abstractions, EF Core configuration, and migrations.
- `Workit.Api.Tests/`: API and contract-style integration tests.
- `Workit.Core.Tests/`: domain, validator, handler, and Core behavior tests.
- `agents/rules/`: project rules for architecture, testing, EF, Git, background jobs, and style.

Endpoint files should be one endpoint per file, named `{Action}Endpoint.cs`, and implement `IRouteMapper`.

## Build, Test, and Development Commands

```bash
dotnet restore
```
Restores NuGet packages.

```bash
dotnet build
```
Builds the full solution.

```bash
dotnet test
```
Runs all xUnit tests.

```bash
docker compose up -d
```
Starts local PostgreSQL and Redis. PostgreSQL is exposed on `localhost:5543`.

```bash
dotnet ef database update --project Workit.Core --startup-project Workit.Api --context AppDbContext
```
Applies EF Core migrations.

```bash
dotnet run --project Workit.Api
```
Runs the API at `http://localhost:5187`.

## Coding Style & Naming Conventions

Use idiomatic C# with nullable reference types enabled. Prefer records for requests/responses and sealed classes for handlers/services. Use PascalCase for types and members, camelCase for locals and parameters, and `I` prefixes for interfaces.

Core use cases follow `Workit.Core/<Feature>/<Action>.cs` with nested `Request`, `Response`, optional `RequestValidator`, and internal sealed `Handler`.

## Testing Guidelines

Tests use xUnit, NSubstitute, and Shouldly. Test classes use the `{FeatureName}Should` naming pattern. Commands and queries that touch the database should have integration coverage. Domain behavior should have focused unit tests. API endpoints should verify status codes, authorization behavior, and response shape.

## Commit & Pull Request Guidelines

Use Conventional Commits with ticket identifiers when available:

```text
feat(WORK-132): Add users pagination endpoint
fix(WORK-142): Correct token expiration handling
```

PRs should include a short description, linked ticket, testing notes, and any migration/configuration impact.

## Security & Configuration

Do not commit secrets. Local settings live in `.env` or `Workit.Api/Properties/launchSettings.json`. JWT secrets, connection strings, CORS origins, and Hangfire settings are read from environment variables.
