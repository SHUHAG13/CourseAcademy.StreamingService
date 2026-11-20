using CourseAcademy.Streaming.Api.Common;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((context, services, logger) => logger
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddStreamingApi(builder.Configuration);

    var app = builder.Build();
    app.UseStreamingApiPipeline();
    await app.MigrateDatabaseAsync();
    await app.RunAsync();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    // HostAbortedException is how `dotnet ef` stops the app after reading the model; it is not a failure.
    Log.Fatal(exception, "The Streaming API failed to start.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Visible to the tests (WebApplicationFactory).</summary>
public partial class Program;
