namespace CourseAcademy.Streaming.Api.Entities;

/// <summary>
/// One transcoding run of a video. A worker leases it, renews the lease while it works, and gives it back when it
/// stops; a lease that runs out (the worker died or its machine was taken away) lets another worker continue.
/// </summary>
public sealed class TranscodeJob : BaseEntity
{
    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public string StatusCode { get; set; } = string.Empty;
    /// <summary>Number of times a worker started this job.</summary>
    public int Attempt { get; set; }
    public int MaxAttempts { get; set; }
    /// <summary>The job waits until this time before it may be leased again (backoff after a failure).</summary>
    public DateTime? NextAttemptAt { get; set; }

    /// <summary>The worker holding the job, and until when.</summary>
    public string? LeaseOwner { get; set; }
    public DateTime? LeaseExpiresAt { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    /// <summary>What the worker is doing now (e.g. probing, encoding) and how far it is, 0–100.</summary>
    public string? Stage { get; set; }
    public int ProgressPercent { get; set; }
    public DateTime? ProgressAt { get; set; }

    /// <summary>Video encoder in use: h264_nvenc on a GPU, libx264 on a CPU.</summary>
    public string? Engine { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Changes on every update, so two workers can never both take the same lease.</summary>
    public byte[] RowVersion { get; set; } = [];

    public List<JobChunk> Chunks { get; set; } = [];
}

/// <summary>
/// A time slice of the video (about 10 minutes) encoded as one piece. Finished chunks are kept, so a job that
/// restarts continues with the first chunk that is not done.
/// </summary>
public sealed class JobChunk : BaseEntity
{
    public int TranscodeJobId { get; set; }
    public TranscodeJob Job { get; set; } = null!;

    /// <summary>0, 1, 2 … in playback order.</summary>
    public int Index { get; set; }
    public decimal StartSeconds { get; set; }
    public decimal LengthSeconds { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
}
