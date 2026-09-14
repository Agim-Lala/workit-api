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

Register a worker:

```http
POST /auth/register/worker
Content-Type: application/json

{
  "email": "worker@example.com",
  "password": "password123",
  "firstName": "Test",
  "lastName": "Worker"
}
```

Register a business:

```http
POST /auth/register/business
Content-Type: application/json

{
  "email": "business@example.com",
  "password": "password123",
  "businessName": "Test Business",
  "fullAddress": "Rruga Test, Tirane",
  "latitude": 41.3275,
  "longitude": 19.8189
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

## Localization

The API answers in English (`en`, default) or Albanian (`sq`). Callers select the
language with the standard `Accept-Language` header; unsupported or missing values
fall back to English.

```http
GET /job-openings
Accept-Language: sq
```

What is localized:

- **Validation errors** – FluentValidation messages and property names.
- **Domain and not-found errors** – the `detail` and `title` of error responses.
- **Enum labels** – responses carry a localized `*Label` next to each enum
  (`jobTypeLabel`, `shiftTypeLabel`, `payTypeLabel`, `statusLabel`, `roleLabel`);
  the raw enum value is unchanged.
- **Job opening free text** – a business may submit per-language `translations`
  when creating a job opening. Reads return the caller's language with field-level
  fallback to the base language (`contentLanguage`, default `en`).

```jsonc
POST /job-openings
{
  "title": "Weekend waiter",
  "description": "Evening dinner service.",
  "role": "Waiter",
  "contentLanguage": "en",
  "translations": {
    "sq": { "title": "Kamarier fundjave", "description": "Shërbim darke.", "role": "Kamarier" }
  }
  // ... remaining job-opening fields
}
```

Translation strings live in `Workit.Core/Shared/Localization/Resources/translations.{en,sq}.json`
(embedded resources). Add a language by extending `Language.Supported` and adding a
matching resource file.

## Worker Profile

All routes below require a worker JWT (`RequireAuthorization(AuthorizationPolicies.WorkerOnly)`).

- `GET /worker-profile` – the full profile: contact info, location (with
  `isLocationVerified`/`country`), CV/photo presence, preferences, and the
  ID-verification badge status.
- `PUT /worker-profile/location` – `{ "location": "Durrës" }`. Looked up against
  OpenStreetMap's free Nominatim API to normalize the city and country; a lookup
  miss or third-party outage saves the text as typed, unverified, rather than
  rejecting the request. No API key required.
- `PUT /worker-profile/preferences` – `{ "interestedFields": ["Bartending"], "preferredShiftTypes": [0, 1] }`
  (see `ShiftType`: `0` Morning, `1` Evening, `2` CustomHours).
- `POST /worker-profile/cv` (multipart, field `file`, PDF, 5MB max) and
  `GET /worker-profile/cv` – upload/download the worker's CV. Files are stored
  on local disk under `IFileStorage`'s configured root (`STORAGE_ROOT_PATH`,
  default `App_Data/uploads`); swap the implementation to move to cloud storage.
- `POST /worker-profile/photo` (multipart, field `file`, JPEG/PNG/WebP, 5MB max)
  and `GET /worker-profile/photo` – upload/download the profile photo.
- `POST /worker-profile/verification/start` – starts a hosted
  [Persona](https://withpersona.com) identity-verification inquiry and returns
  `hostedUrl` to redirect the worker to. Requires `PERSONA_API_KEY` and
  `PERSONA_INQUIRY_TEMPLATE_ID`; without them the endpoint returns a clear
  domain error instead of attempting the call.
- `POST /webhooks/persona` (anonymous, called by Persona) – applies the
  inquiry's outcome to the worker's verification badge. Requires
  `PERSONA_WEBHOOK_SECRET`; every delivery's `Persona-Signature` header is
  verified before the payload is trusted.

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
