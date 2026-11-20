using CourseAcademy.Streaming.Api.Entities;
using CourseAcademy.Streaming.Core.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static CourseAcademy.Streaming.Api.Data.Configurations.ConfigurationExtensions;

namespace CourseAcademy.Streaming.Api.Data.Configurations;

public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.KindCode).HasMaxLength(20).IsRequired();
        builder.Property(e => e.StatusCode).HasMaxLength(20).IsRequired();
        builder.Property(e => e.ExternalRef).HasMaxLength(100).IsRequired();
        builder.Property(e => e.FileName).HasMaxLength(255).IsRequired();
        builder.Property(e => e.MimeType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.RawObjectKey).HasMaxLength(500).IsRequired();
        builder.Property(e => e.ThumbnailObjectKey).HasMaxLength(500);
        builder.Property(e => e.ErrorCode).HasMaxLength(100);
        builder.Property(e => e.ErrorMessage).HasMaxLength(500);
        // Two changes to one asset at the same moment would hand out the same event number; the second save fails instead.
        builder.Property(e => e.LastSequence).IsConcurrencyToken();

        // Not filtered: a public id is never given out twice, not even after a delete.
        builder.HasIndex(e => e.PublicId).IsUnique();
        builder.HasIndex(e => e.StatusCode);
        builder.HasIndex(e => e.ArchiveExpiresAt).HasFilter("[ArchiveExpiresAt] IS NOT NULL");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Asset_Kind", In(nameof(Asset.KindCode), AssetKinds.All));
            t.HasCheckConstraint("CK_Asset_Status", In(nameof(Asset.StatusCode), AssetStatuses.All));
            t.HasCheckConstraint("CK_Asset_Numbers",
                "[SizeBytes] > 0 AND [LastSequence] >= 0 AND ([DurationSeconds] IS NULL OR [DurationSeconds] >= 0)"
                + " AND ([Width] IS NULL OR [Width] > 0) AND ([Height] IS NULL OR [Height] > 0)");
        });
    }
}

public sealed class UploadSessionConfiguration : IEntityTypeConfiguration<UploadSession>
{
    public void Configure(EntityTypeBuilder<UploadSession> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.MultipartUploadId).HasMaxLength(1024).IsRequired();
        builder.HasIndex(e => e.AssetId).IsUnique().HasFilter("[IsDeleted] = 0");
        // The clean-up finds open uploads that ran out.
        builder.HasIndex(e => e.ExpiresAt).HasFilter("[CompletedAt] IS NULL AND [AbortedAt] IS NULL");
        builder.HasOne(e => e.Asset).WithOne(a => a.Upload).HasForeignKey<UploadSession>(e => e.AssetId).OnDelete(DeleteBehavior.NoAction);
        builder.ToTable(t => t.HasCheckConstraint("CK_UploadSession_Parts", "[PartSizeBytes] > 0 AND [PartCount] BETWEEN 1 AND 10000"));
    }
}

public sealed class AssetTrackConfiguration : IEntityTypeConfiguration<AssetTrack>
{
    public void Configure(EntityTypeBuilder<AssetTrack> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.KindCode).HasMaxLength(20).IsRequired();
        builder.Property(e => e.SourceCode).HasMaxLength(20).IsRequired();
        builder.Property(e => e.StatusCode).HasMaxLength(20).IsRequired();
        builder.Property(e => e.LanguageCode).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Label).HasMaxLength(100).IsRequired();
        builder.Property(e => e.SourceObjectKey).HasMaxLength(500);
        builder.Property(e => e.PlaylistPath).HasMaxLength(500);
        builder.Property(e => e.ErrorMessage).HasMaxLength(500);

        builder.HasIndex(e => e.PublicId).IsUnique();
        builder.HasIndex(e => e.AssetId);
        builder.HasOne(e => e.Asset).WithMany(a => a.Tracks).HasForeignKey(e => e.AssetId).OnDelete(DeleteBehavior.NoAction);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_AssetTrack_Kind", In(nameof(AssetTrack.KindCode), TrackKinds.All));
            t.HasCheckConstraint("CK_AssetTrack_Source", In(nameof(AssetTrack.SourceCode), TrackSources.All));
            t.HasCheckConstraint("CK_AssetTrack_Status", In(nameof(AssetTrack.StatusCode), TrackStatuses.All));
            // Embedded tracks point at a stream of the file; uploaded subtitles point at their own file.
            t.HasCheckConstraint("CK_AssetTrack_Origin",
                $"([SourceCode] = '{TrackSources.Embedded}' AND [StreamIndex] IS NOT NULL)"
                + $" OR ([SourceCode] = '{TrackSources.External}' AND [KindCode] = '{TrackKinds.Subtitle}' AND [SourceObjectKey] IS NOT NULL)");
        });
    }
}

public sealed class RenditionConfiguration : IEntityTypeConfiguration<Rendition>
{
    public void Configure(EntityTypeBuilder<Rendition> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Codec).HasMaxLength(30).IsRequired();
        builder.Property(e => e.PlaylistPath).HasMaxLength(500).IsRequired();
        builder.HasIndex(e => new { e.AssetId, e.Height }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasOne(e => e.Asset).WithMany(a => a.Renditions).HasForeignKey(e => e.AssetId).OnDelete(DeleteBehavior.NoAction);
        builder.ToTable(t => t.HasCheckConstraint("CK_Rendition_Numbers", "[Width] > 0 AND [Height] > 0 AND [BitrateKbps] > 0"));
    }
}
