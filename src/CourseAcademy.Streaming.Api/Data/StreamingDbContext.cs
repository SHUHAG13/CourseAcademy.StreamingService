using System.Linq.Expressions;
using CourseAcademy.Streaming.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CourseAcademy.Streaming.Api.Data;

public class StreamingDbContext(DbContextOptions<StreamingDbContext> options) : DbContext(options)
{
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<UploadSession> UploadSessions => Set<UploadSession>();
    public DbSet<AssetTrack> AssetTracks => Set<AssetTrack>();
    public DbSet<Rendition> Renditions => Set<Rendition>();

    public DbSet<TranscodeJob> TranscodeJobs => Set<TranscodeJob>();
    public DbSet<JobChunk> JobChunks => Set<JobChunk>();

    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StreamingDbContext).Assembly);
        ApplySchemas(modelBuilder);
        ApplySoftDeleteFilters(modelBuilder);
    }

    /// <summary>Puts every table in the schema of its area (<see cref="DbSchemas"/>).</summary>
    private static void ApplySchemas(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(e => !e.IsOwned()))
        {
            entityType.SetSchema(DbSchemas.Of(entityType.ClrType));
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // All times are stored as UTC. SQL Server returns them without a kind, so mark them UTC on read
        // so they serialize with a trailing "Z".
        configurationBuilder.Properties<DateTime>().HaveColumnType("datetime2(3)").HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        configurationBuilder.Properties<string>().HaveMaxLength(200);
    }

    /// <summary>Hides soft-deleted rows from every query. Use IgnoreQueryFilters() to see them.</summary>
    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) || entityType.BaseType is not null) continue;
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var body = Expression.Not(Expression.Property(parameter, nameof(BaseEntity.IsDeleted)));
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
}
