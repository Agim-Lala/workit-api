# Workit API

## Commands
Run these from `workit-api/` (the solution lives there, not the repo root):
- Build: `dotnet build`
- Test: `dotnet test` (runs `Workit.Core.Tests` and `Workit.Api.Tests`)

## Architecture
- Repo root (`workit-api/`, this file's directory) just wraps the .NET solution, which lives one level down in `workit-api/workit-api/` (`Workit.sln`, `Workit.Api`, `Workit.Core`, and their `*.Tests` counterparts).
- `Workit.Api` is the HTTP layer (minimal-API endpoint classes), `Workit.Core` holds domain + MediatR request/handler pairs (one static class per use case, e.g. `GetJobOpenings`).
- Worker and business profiles are separate entities from `User`, each 1:1 keyed on `UserId`.

## Conventions
- Always work in this repo with the `caveman` (terse, low-token responses) and `ponytail` (minimal/YAGNI code, no overengineering) skills active — load both at the start of every session here, not just when explicitly asked.

## Gotchas
- `WorkerProfile.Latitude`/`Longitude`/`IsLocationVerified` are only ever set by `UpdateWorkerLocation` (via `ICityLookupService`) — `RegisterWorker` never geocodes. Most workers have an unverified free-text `Location` with no coordinates until they explicitly update it.
- `BusinessProfile.Latitude`/`Longitude` are required at registration, so businesses always have coordinates — unlike workers.
- API tests (`Workit.Api.Tests`) swap `ICityLookupService`/`IIdentityVerificationProvider` for fakes (see `WebApplicationFactory` setup in test files) to avoid hitting Nominatim/Stripe in CI.
