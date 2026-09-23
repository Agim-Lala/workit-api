# Workit API

## Commands
Run these from `workit-api/` (the solution lives there, not the repo root):
- Build: `dotnet build`
- Test: `dotnet test` (runs `Workit.Core.Tests` and `Workit.Api.Tests`)
- Add migration: `dotnet ef migrations add <Name> --project Workit.Core --startup-project Workit.Api --context AppDbContext` (`--context` is required — the solution has two DbContexts, `AppDbContext` and `ReadAppDbContext`). Run `dotnet tool restore` first if `dotnet ef` isn't found.

## Architecture
- Repo root (`workit-api/`, this file's directory) just wraps the .NET solution, which lives one level down in `workit-api/workit-api/` (`Workit.sln`, `Workit.Api`, `Workit.Core`, and their `*.Tests` counterparts).
- `Workit.Api` is the HTTP layer (minimal-API endpoint classes), `Workit.Core` holds domain + MediatR request/handler pairs (one static class per use case, e.g. `GetJobOpenings`).
- Worker and business profiles are separate entities from `User`, each 1:1 keyed on `UserId`.

## Conventions
- Always work in this repo with the `caveman` (terse, low-token responses) and `ponytail` (minimal/YAGNI code, no overengineering) skills active — load both at the start of every session here, not just when explicitly asked.

## Gotchas
- Outbound email goes through Resend (`RESEND_API_KEY`), not SMTP. The confirmation email's subject/HTML live in a Resend dashboard Template (alias `confirm-email`), not in C# — `EmailConfirmationSender` only sends `{confirmationLink, expirationInHours}` variables. `docs/email-templates/confirm-email.html` is a copy of the template source for editing, not something the app reads at runtime.
- Confirmation-email sending is asynchronous: handlers enqueue onto `IEmailConfirmationQueue` (in-memory, `System.Threading.Channels`) and return immediately; `EmailConfirmationBackgroundService` (a hosted service in `Workit.Api`) drains it and does the actual send. Tests must poll `FakeEmailSender.SentEmails`, not assert on it immediately after the HTTP call returns.
- `WorkerProfile.Latitude`/`Longitude`/`IsLocationVerified` are only ever set by `UpdateWorkerLocation` (via `ICityLookupService`) — `RegisterWorker` never geocodes. Most workers have an unverified free-text `Location` with no coordinates until they explicitly update it.
- `BusinessProfile.Latitude`/`Longitude` are required at registration, so businesses always have coordinates — unlike workers.
- API tests (`Workit.Api.Tests`) swap `ICityLookupService`/`IIdentityVerificationProvider` for fakes (see `WebApplicationFactory` setup in test files) to avoid hitting Nominatim/Stripe in CI.
