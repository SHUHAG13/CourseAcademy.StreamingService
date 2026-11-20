# Data model — CourseAcademyStreamingDB

SQL Server, EF Core code-first (`src/CourseAcademy.Streaming.Api/Data`). Same conventions as the Main App:
- Every table has `Id int IDENTITY`, `CreatedAt`, `UpdatedAt`, `IsDeleted` (soft delete, hidden from queries).
- Times are UTC `datetime2(3)`. Strings default to `nvarchar(200)`.
- Tables live in area schemas (`Data/DbSchemas.cs`): `Media`, `Processing`, `Messaging`.
- Codes (kind, status, …) are strings checked by constraints; the values are in `CourseAcademy.Streaming.Core/Media/MediaCodes.cs`.
- Public ids (`Asset.PublicId`, `AssetTrack.PublicId`) are GUIDs; the int `Id` never leaves the service.

Migrations: `InitialCreate` (S1). Add new ones on top; never rewrite one that has been applied.

## Media.Asset
A file kept for the Main App.
| Column | Type | Notes |
|---|---|---|
| PublicId | uniqueidentifier, unique | `assetId` in the contract |
| KindCode | nvarchar(20) | `video`, `document` |
| StatusCode | nvarchar(20) | `pending_upload`, `uploaded`, `queued`, `processing`, `ready`, `failed`, `deleting`, `deleted` |
| ExternalRef | nvarchar(100) | Main App's `MediaAsset.Id` (tracing only) |
| FileName / MimeType / SizeBytes | nvarchar(255) / nvarchar(100) / bigint > 0 | as announced by the Main App |
| RawObjectKey | nvarchar(500) | original in the raw bucket; moves under `archive/` on delete |
| DurationSeconds, Width, Height | int? | from the probe |
| ThumbnailObjectKey | nvarchar(500)? | poster in the HLS bucket |
| HasCustomThumbnail | bit | uploaded by an admin |
| ErrorCode / ErrorMessage | nvarchar(100)? / nvarchar(500)? | when `failed` |
| LastSequence | bigint ≥ 0, concurrency token | number of the last event created |
| ReadyAt, DeleteRequestedAt, ArchiveExpiresAt | datetime2? | archive removed at `ArchiveExpiresAt` (90 days) |

## Media.UploadSession
One multipart upload per asset (unique `AssetId` among non-deleted rows).
| Column | Type | Notes |
|---|---|---|
| AssetId | int FK | |
| MultipartUploadId | nvarchar(1024) | storage's upload id |
| PartSizeBytes / PartCount | bigint > 0 / int 1–10000 | |
| ExpiresAt, CompletedAt, AbortedAt | datetime2 | open sessions past `ExpiresAt` are aborted |

## Media.AssetTrack
Audio or subtitle track of a video.
| Column | Type | Notes |
|---|---|---|
| AssetId | int FK | |
| PublicId | uniqueidentifier, unique | `trackId` in the contract |
| KindCode / SourceCode / StatusCode | nvarchar(20) | `audio`/`subtitle`; `embedded`/`external`; `pending`/`ready`/`failed` |
| StreamIndex | int? | required for embedded tracks |
| LanguageCode | nvarchar(20) | tag from the file (e.g. `ben`, `eng`, `und`) or `bn`/`en` for uploads |
| Label | nvarchar(100) | name in the player's menu; admins can change it |
| IsDefault, IsHidden | bit | |
| SortOrder | int | menu order |
| SourceObjectKey | nvarchar(500)? | required for uploaded subtitles |
| PlaylistPath | nvarchar(500)? | relative to the asset's HLS folder |
| ErrorMessage | nvarchar(500)? | |

## Media.Rendition
One quality level (unique `AssetId + Height` among non-deleted rows).
| Column | Type | Notes |
|---|---|---|
| AssetId | int FK | |
| Width, Height, BitrateKbps | int > 0 | bitrate is the cap |
| Codec | nvarchar(30) | `h264_nvenc` or `libx264` |
| PlaylistPath | nvarchar(500) | relative to the asset's HLS folder |

## Processing.TranscodeJob
One transcoding run of a video, leased by a worker.
| Column | Type | Notes |
|---|---|---|
| AssetId | int FK | |
| StatusCode | nvarchar(20) | `queued`, `running`, `succeeded`, `failed`, `cancelled` |
| Attempt / MaxAttempts | int ≥ 0 / int ≥ 1 | |
| NextAttemptAt | datetime2? | backoff after a failure |
| LeaseOwner / LeaseExpiresAt | nvarchar(100)? / datetime2? | an expired lease lets another worker continue |
| StartedAt, FinishedAt | datetime2? | |
| Stage / ProgressPercent / ProgressAt | nvarchar(30)? / int 0–100 / datetime2? | shown to the Main App on request |
| Engine | nvarchar(30)? | encoder in use |
| ErrorCode / ErrorMessage | nvarchar(100)? / nvarchar(500)? | |
| RowVersion | rowversion | two workers can never take the same lease |

## Processing.JobChunk
A ~10-minute slice encoded as one piece (unique `TranscodeJobId + Index` among non-deleted rows).
| Column | Type | Notes |
|---|---|---|
| TranscodeJobId | int FK | |
| Index | int ≥ 0 | playback order |
| StartSeconds / LengthSeconds | decimal(10,3) | |
| StatusCode | nvarchar(20) | `pending`, `done` |
| CompletedAt | datetime2? | |

## Messaging.OutboxEvent
Webhook events for the Main App, saved with the change they report.
| Column | Type | Notes |
|---|---|---|
| EventId | uniqueidentifier, unique | `webhook-id` header |
| AssetId | int FK | unique with `Sequence` |
| Type | nvarchar(50) | `asset.queued`, `asset.processing`, `asset.ready`, `asset.failed`, `asset.tracks_updated`, `asset.deleted` |
| Sequence | bigint ≥ 1 | per-asset order |
| Payload | nvarchar(max) | exact signed body |
| OccurredAt | datetime2 | |
| Attempts / NextAttemptAt | int ≥ 0 / datetime2 | backoff |
| DeliveredAt / GaveUpAt | datetime2? | given up after `Webhook:RetryHours` (24) |
| LastError | nvarchar(500)? | |
