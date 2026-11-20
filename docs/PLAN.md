# Plan — CourseAcademy Streaming Service

Agreed with the product owner on 2026-11-20. The contract with the Main App is the source of truth:
`CourseAcademy` repository → `docs/integration/streaming-integration.md` (v2). Change it there first.

## 1. Purpose
A separate service that does all heavy media work for Course Academy: multipart upload, probing, transcoding to
HLS with AES-128, audio and subtitle tracks, thumbnails, playback and download links, archive. The Main App only
decides who may upload, watch or download.

## 2. Decisions
| Topic | Decision |
|---|---|
| Repository | This repository, https://github.com/SHUHAG13/CourseAcademy.StreamingService |
| Stack | .NET 10: Web API + Worker Service, EF Core, SQL Server (`CourseAcademyStreamingDB`) |
| Hosting | Small always-on VPS (Docker compose: API + DB + optional CPU worker as backup) and on-demand GPU workers on RunPod Secure Cloud |
| Speed | Faster than real time on GPU (NVENC). Planned volume ~100 hours of video per month |
| Local development | Docker Desktop with MinIO instead of R2; local SQL Server |
| Uploads | Multipart, resumable. Video ≤ 50 GB (MP4, WebM, MOV, MKV), ≤ 5 hours. Document ≤ 50 MB (PDF, DOCX, PPTX) |
| Documents | Stored and downloaded through signed links only; no processing |
| Ladder | 360/480/720/1080/1440 (no upscaling), H.264 + AAC, fMP4 segments, capped quality; caps 0.8/1.4/2.8/5/8 Mbps |
| Audio | Every embedded audio track becomes its own HLS audio rendition |
| Subtitles | Embedded text subtitles and uploaded `.srt`/`.vtt`; uploaded ones in Bangla or English (chosen from a list); on by default; can be added or removed after the video is ready |
| Tracks | Admins can rename and hide tracks |
| Thumbnail | Made from the video automatically; admins can upload their own; shown in the editor and as the player's poster |
| Progress | Main App shows status + % every 3 seconds while the upload section is open |
| Delete | HLS output removed; the original moves to archive for 90 days, then is removed; no restore |
| Security | Playback token (10 minutes, renewed silently by the player) + AES-128 HLS encryption |
| Gateway | Cloudflare Worker (TypeScript) checks the token and serves segments and keys from R2 |
| Main → Streaming | HTTPS + `X-Api-Key`, two keys valid at once for rotation |
| Streaming → Main | Outbox + Standard Webhooks signature, retried for 24 hours |

## 3. Rules (SS-xx)
| ID | Rule |
|---|---|
| SS-01 | `X-Api-Key` auth with up to two valid keys; workers use their own token. |
| SS-02 | Multipart upload: session, part URLs, resume (list of finished parts), complete, abort. Unfinished uploads are cleaned up. |
| SS-03 | Status `pending_upload → uploaded → queued → processing → ready / failed`, and `deleting → deleted`. Documents go `uploaded → ready`. |
| SS-04 | Probe: needs a video stream and a length of 1 second to 5 hours (`DURATION_TOO_LONG`). Image-based subtitles are skipped with a reason. |
| SS-05 | Ladder without upscaling, aspect kept; 6-second segments, 2-second GOP; caps 0.8/1.4/2.8/5/8 Mbps. |
| SS-06 | Each embedded audio track is a separate rendition. |
| SS-07 | Subtitles: embedded and uploaded (`.srt`/`.vtt`, `bn`/`en`) → WebVTT in sync; adding or removing one after ready only rewrites playlists. |
| SS-08 | Tracks can be renamed and hidden. |
| SS-09 | Automatic thumbnail (skips black frames) and custom thumbnail upload. |
| SS-10 | Encoding in ~10-minute chunks with checkpoints; GPU (NVENC) when present, otherwise CPU. |
| SS-11 | Progress: stage and %, written about every 5 seconds. |
| SS-12 | AES-128 with one key per asset, kept in a separate private bucket. |
| SS-13 | Path-token playback; the Cloudflare Worker checks the token, serves segments and keys, handles CORS, caches without the token. |
| SS-14 | Outbox + Standard Webhooks; a failed callback never causes a new transcode. |
| SS-15 | Delete: HLS removed, original archived for 90 days, then removed. No restore. |
| SS-16 | RunPod GPU workers start when jobs wait and stop after the queue has been empty for 10 minutes. |

Out of scope: DRM, watermarks, live streaming, H.265/AV1, archive restore.

## 4. Steps
| Step | Streaming service | Main App (CourseAcademy repository) | Status |
|---|---|---|---|
| S1 | Repository structure, SQL Server database and first migration, API key auth, health, docs | Contract v2 document | Done 2026-11-20 |
| S2 | Multipart upload, assets, document download, delete/archive, outbox webhooks | Multipart upload (API + UI), webhook format | — |
| S3 | Worker: probe, ladder, chunked encoding, audio, thumbnail, progress | Progress % | — |
| S4 | Subtitles, track management, custom thumbnail | Subtitle and track UI, thumbnail | — |
| S5 | AES-128, playback tokens, Cloudflare Worker | Player menus, poster, silent token renewal | — |
| S6 | RunPod launcher, Docker images, VPS deployment | Deployment settings | — |

## 5. Acceptance (whole project)
- A 5-hour MKV with 2 audio and 3 subtitle tracks becomes ready faster than real time on GPU and plays in hls.js and Safari.
- A worker stopped mid-way continues from the last finished chunk.
- Segments and keys without a valid token answer 403.
- Webhooks reach the Main App later even if it was down.
- `dotnet test` passes.
