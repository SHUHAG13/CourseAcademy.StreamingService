namespace CourseAcademy.Streaming.Api.Entities;

/// <summary>
/// An event for the Main App (outbox). It is saved in the same transaction as the change it reports, then a
/// background sender delivers it as a signed webhook and retries with backoff until it is accepted or too old.
/// </summary>
public sealed class OutboxEvent : BaseEntity
{
    /// <summary>The webhook-id header; the Main App ignores an id it has seen before.</summary>
    public Guid EventId { get; set; }
    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    /// <summary>asset.queued, asset.processing, asset.ready, asset.failed, asset.tracks_updated or asset.deleted.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Per-asset order; the Main App applies only events newer than the last one it applied.</summary>
    public long Sequence { get; set; }
    /// <summary>The exact JSON body that is signed and sent.</summary>
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }

    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    /// <summary>Set when the retry window passed without the Main App accepting the event.</summary>
    public DateTime? GaveUpAt { get; set; }
    public string? LastError { get; set; }
}
