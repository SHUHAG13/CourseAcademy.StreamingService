# Changelog

Format: `YYYY-MM-DD | <area> | <what changed> | <rule IDs affected>` — newest first.

2026-11-20 | all | Step S1: repository created. .NET 10 solution with Api, Worker, Core and Tests projects. SQL Server database `CourseAcademyStreamingDB` with schemas Media (Asset, UploadSession, AssetTrack, Rendition), Processing (TranscodeJob, JobChunk) and Messaging (OutboxEvent), migration `InitialCreate`. `X-Api-Key` authentication with up to two keys on every endpoint except `/health`; `GET /internal/v1/ping`; error body `{ error: { code, message, fields } }`. Settings checked at start-up (keys, storage, webhook; https outside Development). Worker checks FFmpeg/ffprobe and NVENC at start-up. MinIO compose file for local development. | SS-01
