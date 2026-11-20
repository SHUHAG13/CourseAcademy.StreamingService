using CourseAcademy.Streaming.Api.Data;
using CourseAcademy.Streaming.Api.Entities;
using CourseAcademy.Streaming.Core.Media;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Streaming.Tests.Data;

public class SchemaTests
{
    [Fact]
    public void Every_table_has_an_area_schema()
    {
        var (db, _) = TestDb.Create();
        var tables = db.Model.GetEntityTypes().Select(e => (Table: e.GetTableName(), Schema: e.GetSchema())).ToList();

        Assert.All(tables, t => Assert.Contains(t.Schema, DbSchemas.All));
        Assert.Contains(("Asset", DbSchemas.Media), tables);
        Assert.Contains(("UploadSession", DbSchemas.Media), tables);
        Assert.Contains(("AssetTrack", DbSchemas.Media), tables);
        Assert.Contains(("Rendition", DbSchemas.Media), tables);
        Assert.Contains(("TranscodeJob", DbSchemas.Processing), tables);
        Assert.Contains(("JobChunk", DbSchemas.Processing), tables);
        Assert.Contains(("OutboxEvent", DbSchemas.Messaging), tables);
    }

    [Fact]
    public async Task Saving_sets_the_audit_times_and_delete_is_soft()
    {
        var (db, clock) = TestDb.Create();
        var asset = new Asset
        {
            PublicId = Guid.NewGuid(), KindCode = AssetKinds.Video, StatusCode = AssetStatuses.PendingUpload, ExternalRef = "41",
            FileName = "lesson.mkv", MimeType = "video/x-matroska", SizeBytes = 1024, RawObjectKey = "videos/x/original",
        };
        db.Assets.Add(asset);
        await db.SaveChangesAsync();
        Assert.Equal(TestDb.Start.UtcDateTime, asset.CreatedAt);

        clock.Advance(TimeSpan.FromMinutes(5));
        db.Assets.Remove(asset);
        await db.SaveChangesAsync();

        Assert.True(asset.IsDeleted);
        Assert.Equal(TestDb.Start.UtcDateTime.AddMinutes(5), asset.UpdatedAt);
        Assert.Empty(await db.Assets.ToListAsync());
        Assert.Single(await db.Assets.IgnoreQueryFilters().ToListAsync());
    }
}
