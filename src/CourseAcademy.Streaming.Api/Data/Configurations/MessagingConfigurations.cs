using CourseAcademy.Streaming.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseAcademy.Streaming.Api.Data.Configurations;

public sealed class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ConfigureBase();
        builder.Property(e => e.Type).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Payload).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(e => e.LastError).HasMaxLength(500);

        builder.HasIndex(e => e.EventId).IsUnique();
        builder.HasIndex(e => new { e.AssetId, e.Sequence }).IsUnique();
        // The sender reads events that are still to be delivered, oldest wait first.
        builder.HasIndex(e => e.NextAttemptAt).HasFilter("[DeliveredAt] IS NULL AND [GaveUpAt] IS NULL");
        builder.HasOne(e => e.Asset).WithMany().HasForeignKey(e => e.AssetId).OnDelete(DeleteBehavior.NoAction);

        builder.ToTable(t => t.HasCheckConstraint("CK_OutboxEvent_Numbers", "[Sequence] >= 1 AND [Attempts] >= 0"));
    }
}
