# Course Academy Streaming Service

Media service for Course Academy (Bangladesh online academy): multipart uploads, FFmpeg transcoding to HLS
(AES-128, multi-audio, subtitles, thumbnails), signed playback and download links, archive. Works only for the
Main App, which owns users, courses and permissions. Main App repository: `../CourseAcademy`
(https://github.com/SHUHAG13/CourseAcademy).

## Stack
- .NET 10: ASP.NET Core Web API (`src/CourseAcademy.Streaming.Api`), Worker Service (`src/CourseAcademy.Streaming.Worker`), shared library (`src/CourseAcademy.Streaming.Core`)
- EF Core + SQL Server (`CourseAcademyStreamingDB`), migrations in `Api/Data/Migrations` (`InitialCreate`; add new ones on top, never rewrite applied ones)
- Serilog, Swashbuckle + Scalar (Development)
- FFmpeg/ffprobe (worker), S3-compatible storage (Cloudflare R2; MinIO locally via `deploy/docker-compose.dev.yml`)
- Planned: Cloudflare Worker gateway in TypeScript (`playback-gateway/`, step S5), RunPod GPU workers (S6)
- xUnit tests (`tests/CourseAcademy.Streaming.Tests`)

## Commands
- `dotnet build` / `dotnet test` (repository root)
- `dotnet run --project src/CourseAcademy.Streaming.Api --launch-profile http` — API on http://localhost:5180 (needs user secrets, see README)
- `dotnet run --project src/CourseAcademy.Streaming.Worker`
- `docker compose -f deploy/docker-compose.dev.yml up -d` — MinIO

## Conventions
- Same layering and conventions as the Main App's API: Controller → Service → Repository, `BaseEntity` with soft delete, UTC `datetime2(3)`, tables in area schemas (`Data/DbSchemas.cs`), warnings are errors.
- Internal API under `/internal/v1`; success = plain DTO; failure = `{ error: { code, message, fields } }`.
- The worker never touches the database; it talks to the API.
- Codes (kinds, statuses) live in `Core/Media/MediaCodes.cs` and must match the contract.

## Task Workflow (MANDATORY)
For every new task or change request, do NOT start coding right away:
1. **Write a proper prompt first**: goal, scope (files, endpoints), requirements and rules (SS-IDs), out of scope, acceptance criteria, open questions.
2. **Ask the user to confirm**: show the prompt and ask "Shall I implement this?"
3. **Only after the user says yes, write the code.** Then update the docs.

Exception: questions or explanations that change no files.

### No assumptions (MANDATORY)
- Never assume anything about business logic, requirements, or technical decisions.
- If anything is unclear or could be read more than one way: stop and ask the user (in Bangla, with easy explanations).
- Do not fill gaps with guesses; list them as open questions.

## Documentation (MANDATORY)
- The contract with the Main App is `../CourseAcademy/docs/integration/streaming-integration.md`. Any change to endpoints, events, statuses or limits is made there first, together with the Main App's docs.
- This repository: `docs/PLAN.md` (decisions, SS rules, step status), `docs/data-model.md` (tables), `docs/CHANGELOG.md` (newest first: `YYYY-MM-DD | <area> | <what changed> | <rule IDs>`), `README.md` (setup and configuration).
- Edit only the changed parts; never rewrite whole files.
- End every reply with: "Docs updated: <files>" or "Docs: no change".
