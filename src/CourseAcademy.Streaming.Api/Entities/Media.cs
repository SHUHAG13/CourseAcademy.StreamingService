namespace CourseAcademy.Streaming.Api.Entities;

/// <summary>
/// A file this service keeps for the Main App: a video (transcoded to HLS) or a document (stored and downloaded as it is).
/// Codes come from CourseAcademy.Streaming.Core.Media.
/// </summary>
public sealed class Asset : BaseEntity
{
    /// <summary>The id shared with the Main App (its MediaAsset.AssetId). The int <see cref="BaseEntity.Id"/> never leaves this service.</summary>
    public Guid PublicId { get; set; }
    public string KindCode { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;

    /// <summary>The Main App's own id for this file, for support and tracing only.</summary>
    public string ExternalRef { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    /// <summary>Key of the original in the raw bucket. It is kept after processing and moves under archive/ when the asset is deleted.</summary>
    public string RawObjectKey { get; set; } = string.Empty;

    /// <summary>Video facts found by the probe.</summary>
    public int? DurationSeconds { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }

    /// <summary>Key of the poster image in the HLS bucket: made from the video, or uploaded by an admin.</summary>
    public string? ThumbnailObjectKey { get; set; }
    public bool HasCustomThumbnail { get; set; }

    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Sequence number of the last event created for this asset; events are numbered 1, 2, 3 … per asset.</summary>
    public long LastSequence { get; set; }

    public DateTime? ReadyAt { get; set; }
    public DateTime? DeleteRequestedAt { get; set; }
    /// <summary>When the archived original is removed for good (90 days after the delete).</summary>
    public DateTime? ArchiveExpiresAt { get; set; }

    public UploadSession? Upload { get; set; }
    public List<AssetTrack> Tracks { get; set; } = [];
    public List<Rendition> Renditions { get; set; } = [];
}

/// <summary>The multipart upload of an asset's original file. One per asset.</summary>
public sealed class UploadSession : BaseEntity
{
    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    /// <summary>The storage's multipart upload id.</summary>
    public string MultipartUploadId { get; set; } = string.Empty;
    public long PartSizeBytes { get; set; }
    public int PartCount { get; set; }

    /// <summary>After this the upload is aborted and the parts already sent are removed.</summary>
    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? AbortedAt { get; set; }
}

/// <summary>An audio or subtitle track of a video, found in the file or uploaded separately.</summary>
public sealed class AssetTrack : BaseEntity
{
    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    /// <summary>Public id the Main App uses to rename, hide or remove the track.</summary>
    public Guid PublicId { get; set; }
    public string KindCode { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;

    /// <summary>Stream index in the original file (embedded tracks only).</summary>
    public int? StreamIndex { get; set; }
    /// <summary>Language tag as found in the file (e.g. "ben", "eng", "und"), or bn/en for uploaded subtitles.</summary>
    public string LanguageCode { get; set; } = string.Empty;
    /// <summary>Name shown in the player's menu; admins can change it.</summary>
    public string Label { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsHidden { get; set; }
    /// <summary>Order in the player's menu.</summary>
    public int SortOrder { get; set; }

    /// <summary>Uploaded subtitle file in the raw bucket (external tracks only).</summary>
    public string? SourceObjectKey { get; set; }
    /// <summary>The track's playlist, relative to the asset's HLS folder.</summary>
    public string? PlaylistPath { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>One quality level of a video's HLS ladder (360p … 1440p).</summary>
public sealed class Rendition : BaseEntity
{
    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public int Width { get; set; }
    public int Height { get; set; }
    /// <summary>The bitrate cap of this level.</summary>
    public int BitrateKbps { get; set; }
    /// <summary>Encoder used, e.g. h264_nvenc or libx264.</summary>
    public string Codec { get; set; } = string.Empty;
    /// <summary>The level's playlist, relative to the asset's HLS folder.</summary>
    public string PlaylistPath { get; set; } = string.Empty;
}
