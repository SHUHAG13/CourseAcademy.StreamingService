using CourseAcademy.Streaming.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CourseAcademy.Streaming.Tests;

/// <summary>
/// The real API in memory, in a "Testing" environment (so the production rules apply), with an in-memory database
/// and test settings instead of user secrets.
/// </summary>
public sealed class StreamingApiFactory : WebApplicationFactory<Program>
{
    public const string FirstKey = "test-key-one-0123456789abcdef0123456789";
    public const string SecondKey = "test-key-two-0123456789abcdef0123456789";

    private readonly Dictionary<string, string?> settings = new()
    {
        ["ConnectionStrings:DefaultConnection"] = "Server=not-used-by-tests",
        ["Database:MigrateOnStartup"] = "false",
        ["Auth:ApiKeys:0"] = FirstKey,
        ["Auth:ApiKeys:1"] = SecondKey,
        ["Storage:ServiceUrl"] = "https://storage.test",
        ["Storage:AccessKey"] = "test-access",
        ["Storage:SecretKey"] = "test-secret",
        ["Storage:RawBucket"] = "test-raw",
        ["Storage:HlsBucket"] = "test-hls",
        ["Storage:KeysBucket"] = "test-keys",
        ["Webhook:Url"] = "https://main.test/api/v1/internal/streaming/webhooks",
        ["Webhook:Secret"] = "whsec_" + Convert.ToBase64String(new byte[32]),
    };

    /// <summary>Changes one setting before the app starts.</summary>
    public StreamingApiFactory With(string key, string? value)
    {
        settings[key] = value;
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        foreach (var (key, value) in settings) builder.UseSetting(key, value);

        var databaseName = Guid.NewGuid().ToString();
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<StreamingDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<StreamingDbContext>>();
            services.AddDbContext<StreamingDbContext>(options => options.UseInMemoryDatabase(databaseName));
        });
    }
}
