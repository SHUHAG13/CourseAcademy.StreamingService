using CourseAcademy.Streaming.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Time.Testing;

namespace CourseAcademy.Streaming.Tests;

/// <summary>An in-memory StreamingDbContext with the real interceptor and a controllable clock.</summary>
internal static class TestDb
{
    public static readonly DateTimeOffset Start = new(2026, 11, 20, 8, 0, 0, TimeSpan.Zero);

    public static (StreamingDbContext Db, FakeTimeProvider Clock) Create()
    {
        var clock = new FakeTimeProvider(Start);
        var options = new DbContextOptionsBuilder<StreamingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(new AuditInterceptor(clock))
            .Options;
        var db = new StreamingDbContext(options);
        db.Database.EnsureCreated();
        return (db, clock);
    }
}
