using CourseAcademy.Streaming.Api.Entities;
using CourseAcademy.Streaming.Core.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static CourseAcademy.Streaming.Api.Data.Configurations.ConfigurationExtensions;

namespace CourseAcademy.Streaming.Api.Data.Configurations;

public sealed class TranscodeJobConfiguration : IEntityTypeConfiguration<TranscodeJob>
{
    public void Configure(EntityTypeBuilder<TranscodeJob> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.StatusCode).HasMaxLength(20).IsRequired();
        builder.Property(e => e.LeaseOwner).HasMaxLength(100);
        builder.Property(e => e.Stage).HasMaxLength(30);
        builder.Property(e => e.Engine).HasMaxLength(30);
        builder.Property(e => e.ErrorCode).HasMaxLength(100);
        builder.Property(e => e.ErrorMessage).HasMaxLength(500);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasIndex(e => e.AssetId);
        // Workers look for queued jobs whose wait is over, and for running jobs whose lease ran out.
        builder.HasIndex(e => new { e.StatusCode, e.NextAttemptAt });
        builder.HasIndex(e => new { e.StatusCode, e.LeaseExpiresAt });
        builder.HasOne(e => e.Asset).WithMany().HasForeignKey(e => e.AssetId).OnDelete(DeleteBehavior.NoAction);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_TranscodeJob_Status", In(nameof(TranscodeJob.StatusCode), JobStatuses.All));
            t.HasCheckConstraint("CK_TranscodeJob_Numbers",
                "[Attempt] >= 0 AND [MaxAttempts] >= 1 AND [ProgressPercent] BETWEEN 0 AND 100");
        });
    }
}

public sealed class JobChunkConfiguration : IEntityTypeConfiguration<JobChunk>
{
    public void Configure(EntityTypeBuilder<JobChunk> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.StatusCode).HasMaxLength(20).IsRequired();
        // Up to 5 hours, to the millisecond.
        builder.Property(e => e.StartSeconds).HasPrecision(10, 3);
        builder.Property(e => e.LengthSeconds).HasPrecision(10, 3);

        builder.HasIndex(e => new { e.TranscodeJobId, e.Index }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasOne(e => e.Job).WithMany(j => j.Chunks).HasForeignKey(e => e.TranscodeJobId).OnDelete(DeleteBehavior.NoAction);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_JobChunk_Status", In(nameof(JobChunk.StatusCode), ChunkStatuses.All));
            t.HasCheckConstraint("CK_JobChunk_Numbers", "[Index] >= 0 AND [StartSeconds] >= 0 AND [LengthSeconds] > 0");
        });
    }
}
