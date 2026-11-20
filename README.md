# Course Academy Streaming Service

The media service of [Course Academy](https://github.com/SHUHAG13/CourseAcademy). It keeps the course files (videos
and documents), transcodes videos to HLS, and hands out short-lived playback and download links. It knows nothing
about users or courses: the Main App decides who may upload, watch or download, then asks this service.

- Contract with the Main App (source of truth): `docs/integration/streaming-integration.md` in the CourseAcademy repository.
- Plan, decisions and steps: [docs/PLAN.md](docs/PLAN.md). Database: [docs/data-model.md](docs/data-model.md). History: [docs/CHANGELOG.md](docs/CHANGELOG.md).

Status: **step S1 of 6** — structure, database, authentication and health check. Uploads, transcoding and playback follow in S2–S6.

## Structure
| Project | Holds |
|---|---|
| `src/CourseAcademy.Streaming.Api` | ASP.NET Core 10 Web API for the Main App (`/internal/v1/...`). Controller → Service → Repository, EF Core, SQL Server. Holds the database. |
| `src/CourseAcademy.Streaming.Worker` | .NET Worker Service that runs FFmpeg. Talks only to the API (never to the database), so it can run on a GPU machine elsewhere. |
| `src/CourseAcademy.Streaming.Core` | Code both use: codes (kinds, statuses), storage settings. |
| `tests/CourseAcademy.Streaming.Tests` | xUnit tests. |
| `deploy/` | Docker compose for local development (MinIO). Production images come in S6. |

## Conventions
- Successful responses are the plain DTO (no envelope). Failures: `{ "error": { "code", "message", "fields": [{ "field", "message" }] } }` with a stable UPPER_SNAKE `code`.
- Every endpoint needs the `X-Api-Key` header except `GET /health`.
- Times are UTC (`datetime2(3)`); every table soft-deletes (`IsDeleted`); tables live in the schemas `Media`, `Processing`, `Messaging`.
- Warnings are errors (`TreatWarningsAsErrors`, in `Directory.Build.props`).

## Requirements
- .NET 10 SDK
- SQL Server (Development uses the local default instance: `Server=.;Database=CourseAcademyStreamingDB;…`)
- FFmpeg and ffprobe on PATH (for the worker)
- Docker Desktop (for MinIO, needed from step S2)

## First-time setup
The API refuses to start without its secrets. Create them once with user secrets (PowerShell, in the repository folder):

```powershell
function New-Secret { $b = New-Object byte[] 32; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); $b }

# Key the Main App sends in X-Api-Key (64 hex characters).
$apiKey = (New-Secret | ForEach-Object { $_.ToString("x2") }) -join ""
dotnet user-secrets set "Auth:ApiKeys:0" $apiKey --project src/CourseAcademy.Streaming.Api

# Secret that signs the webhooks (Standard Webhooks format).
$webhookSecret = "whsec_" + [Convert]::ToBase64String((New-Secret))
dotnet user-secrets set "Webhook:Secret" $webhookSecret --project src/CourseAcademy.Streaming.Api

"Give the Main App these two values:"; $apiKey; $webhookSecret
```

Storage in Development is the local MinIO (settings already in `appsettings.Development.json`):

```powershell
docker compose -f deploy/docker-compose.dev.yml up -d
```
MinIO console: http://localhost:9001 (user `courseacademy`, password `courseacademy-dev-only`). The buckets
`course-raw`, `course-hls` and `course-keys` are created automatically.

## Run locally
```powershell
dotnet run --project src/CourseAcademy.Streaming.Api --launch-profile http
```
- API: http://localhost:5180/internal/v1 — testing page: Scalar http://localhost:5180/scalar (enter the API key under Authentication) or Swagger http://localhost:5180/swagger — health: http://localhost:5180/health
- In Development the database `CourseAcademyStreamingDB` is created and migrated at start-up (`Database:MigrateOnStartup`).
- Logs: console and `src/CourseAcademy.Streaming.Api/logs/streaming-api-<date>.log`.

Worker (for now it only checks FFmpeg and reports whether NVENC is built in):
```powershell
dotnet run --project src/CourseAcademy.Streaming.Worker
```

## Tests
```powershell
dotnet test
```

## Configuration
| Setting | Meaning |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server database of this service |
| `Database:MigrateOnStartup` | Apply migrations at start-up (Development only by default) |
| `Auth:ApiKeys` | One or two keys (32+ characters) the Main App may send. Two allow changing the key without downtime. **Secret** |
| `Storage:ServiceUrl`, `Region`, `ForcePathStyle` | S3 endpoint: R2 (`https://<account>.r2.cloudflarestorage.com`, region `auto`) or MinIO (`http://localhost:9000`, path style) |
| `Storage:AccessKey`, `Storage:SecretKey` | Storage credentials. **Secret** outside Development |
| `Storage:RawBucket`, `HlsBucket`, `KeysBucket` | Originals, HLS output, AES keys — three different buckets |
| `Webhook:Url` | The Main App's webhook endpoint |
| `Webhook:Secret` | `whsec_` + base64 of 24–64 random bytes. **Secret** |
| `Webhook:RetryHours` | How long undelivered events are retried (default 24) |
| `Worker:FfmpegPath`, `FfprobePath`, `WorkDirectory` | Worker tools and scratch folder |

Outside Development, `Webhook:Url` and `Storage:ServiceUrl` must use https, and secrets come from environment
variables (e.g. `Auth__ApiKeys__0`, `Webhook__Secret`, `Storage__SecretKey`).

## Migrations
```powershell
dotnet ef migrations add <Name> --project src/CourseAcademy.Streaming.Api --output-dir Data/Migrations
```
Current: `InitialCreate`. Add new migrations on top; never rewrite one that has been applied.
