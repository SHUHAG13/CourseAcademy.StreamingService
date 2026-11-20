namespace CourseAcademy.Streaming.Core.Media;

// Codes stored in the database and sent to the Main App. The contract (CourseAcademy repository,
// docs/integration/streaming-integration.md) lists the same values; change both together.

public static class AssetKinds
{
    /// <summary>A video, transcoded to HLS.</summary>
    public const string Video = "video";
    /// <summary>A document (PDF, DOCX, PPTX), stored and downloaded as it is.</summary>
    public const string Document = "document";

    public static readonly IReadOnlyList<string> All = [Video, Document];
}

public static class AssetStatuses
{
    public const string PendingUpload = "pending_upload";
    public const string Uploaded = "uploaded";
    public const string Queued = "queued";
    public const string Processing = "processing";
    public const string Ready = "ready";
    public const string Failed = "failed";
    public const string Deleting = "deleting";
    public const string Deleted = "deleted";

    public static readonly IReadOnlyList<string> All = [PendingUpload, Uploaded, Queued, Processing, Ready, Failed, Deleting, Deleted];
}

public static class TrackKinds
{
    public const string Audio = "audio";
    public const string Subtitle = "subtitle";

    public static readonly IReadOnlyList<string> All = [Audio, Subtitle];
}

public static class TrackSources
{
    /// <summary>Found inside the uploaded video file.</summary>
    public const string Embedded = "embedded";
    /// <summary>Uploaded separately (.srt or .vtt subtitle).</summary>
    public const string External = "external";

    public static readonly IReadOnlyList<string> All = [Embedded, External];
}

public static class TrackStatuses
{
    public const string Pending = "pending";
    public const string Ready = "ready";
    public const string Failed = "failed";

    public static readonly IReadOnlyList<string> All = [Pending, Ready, Failed];
}

/// <summary>Languages an external subtitle can be uploaded in. Embedded tracks keep the language tag found in the file.</summary>
public static class SubtitleLanguages
{
    public const string Bangla = "bn";
    public const string English = "en";

    public static readonly IReadOnlyList<string> All = [Bangla, English];
}

public static class JobStatuses
{
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";

    public static readonly IReadOnlyList<string> All = [Queued, Running, Succeeded, Failed, Cancelled];
}

public static class ChunkStatuses
{
    public const string Pending = "pending";
    public const string Done = "done";

    public static readonly IReadOnlyList<string> All = [Pending, Done];
}
