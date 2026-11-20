using CourseAcademy.Streaming.Api.Entities;

namespace CourseAcademy.Streaming.Api.Data;

/// <summary>
/// SQL Server schema of every table, grouped by area (e.g. <c>Media.Asset</c>, <c>Processing.TranscodeJob</c>).
/// Every entity must be listed: the model fails to build otherwise, so a new table never lands in <c>dbo</c> by accident.
/// </summary>
public static class DbSchemas
{
    public const string Media = "Media";
    public const string Processing = "Processing";
    public const string Messaging = "Messaging";

    private static readonly Dictionary<Type, string> Map = new()
    {
        // Files, their uploads, tracks and quality levels.
        [typeof(Asset)] = Media,
        [typeof(UploadSession)] = Media,
        [typeof(AssetTrack)] = Media,
        [typeof(Rendition)] = Media,

        // Transcoding work for the workers.
        [typeof(TranscodeJob)] = Processing,
        [typeof(JobChunk)] = Processing,

        // Events for the Main App.
        [typeof(OutboxEvent)] = Messaging,
    };

    public static IReadOnlyCollection<string> All => [Media, Processing, Messaging];

    public static string Of(Type entityType) =>
        Map.TryGetValue(entityType, out var schema)
            ? schema
            : throw new InvalidOperationException($"No database schema is set for {entityType.Name}. Add it to {nameof(DbSchemas)}.");
}
