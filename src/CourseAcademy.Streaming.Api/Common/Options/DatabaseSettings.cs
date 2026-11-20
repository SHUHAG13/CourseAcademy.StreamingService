namespace CourseAcademy.Streaming.Api.Common.Options;

/// <summary>Section "Database" in configuration.</summary>
public sealed class DatabaseSettings
{
    public const string SectionName = "Database";

    /// <summary>Apply pending migrations at start-up. Meant for development; production applies migrations as a release step.</summary>
    public bool MigrateOnStartup { get; set; }
}
