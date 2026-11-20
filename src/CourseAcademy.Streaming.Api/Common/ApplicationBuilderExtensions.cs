using CourseAcademy.Streaming.Api.Common.Auth;
using CourseAcademy.Streaming.Api.Common.Middleware;
using CourseAcademy.Streaming.Api.Common.Options;
using CourseAcademy.Streaming.Api.Data;
using CourseAcademy.Streaming.Api.Dtos;
using CourseAcademy.Streaming.Core.Storage;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

namespace CourseAcademy.Streaming.Api.Common;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseStreamingApiPipeline(this WebApplication app)
    {
        RequireHttpsOutsideDevelopment(app);

        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            await next();
        });
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        // Requests the caller cancelled are not errors.
        app.UseSerilogRequestLogging(options => options.GetLevel = (context, _, exception) =>
            context.RequestAborted.IsCancellationRequested ? LogEventLevel.Debug
            : exception is not null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError ? LogEventLevel.Error
            : LogEventLevel.Information);

        // Empty error responses (missing key, unknown route, wrong method) get the standard error body too.
        app.UseStatusCodePages(async context =>
        {
            var response = context.HttpContext.Response;
            if (response.ContentLength is > 0 || response.ContentType is not null) return;
            var body = response.StatusCode switch
            {
                StatusCodes.Status401Unauthorized => ErrorResponse.Of(ErrorCodes.Unauthorized,
                    $"Send a valid key in the {ApiKeyAuthenticationHandler.HeaderName} header."),
                StatusCodes.Status403Forbidden => ErrorResponse.Of(ErrorCodes.Forbidden, "This key may not do this."),
                StatusCodes.Status404NotFound => ErrorResponse.Of(ErrorCodes.NotFound, "The requested resource was not found."),
                StatusCodes.Status405MethodNotAllowed => ErrorResponse.Of(ErrorCodes.MethodNotAllowed, "This method is not allowed here."),
                _ => ErrorResponse.Of(ErrorCodes.Unexpected, ReasonPhrases.GetReasonPhrase(response.StatusCode)),
            };
            await response.WriteAsJsonAsync(body);
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Course Academy Streaming API v1"));
            // API testing page at /scalar; it reads the same OpenAPI document as Swagger UI.
            app.MapScalarApiReference(options => options
                    .WithTitle("Course Academy Streaming API")
                    .WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json")
                    .AddPreferredSecuritySchemes(ApiKeyAuthenticationHandler.SchemeName)
                    .EnablePersistentAuthentication())
                .AllowAnonymous();
        }
        else
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(
                new HealthDto(report.Status.ToString(), report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString()))),
        }).AllowAnonymous();
        return app;
    }

    /// <summary>
    /// Outside Development the webhooks (signed, but carrying file details) and the storage traffic (credentials)
    /// must go over https. Plain http is only for the local Main App and MinIO.
    /// </summary>
    private static void RequireHttpsOutsideDevelopment(WebApplication app)
    {
        if (app.Environment.IsDevelopment()) return;
        var webhook = app.Services.GetRequiredService<IOptions<WebhookSettings>>().Value;
        var storage = app.Services.GetRequiredService<IOptions<StorageSettings>>().Value;
        if (!webhook.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Webhook:Url must use https outside Development.");
        }
        if (!storage.ServiceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Storage:ServiceUrl must use https outside Development.");
        }
    }

    /// <summary>Applies pending migrations when Database:MigrateOnStartup is true (development only by default).</summary>
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        if (!app.Services.GetRequiredService<IOptions<DatabaseSettings>>().Value.MigrateOnStartup) return;
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<StreamingDbContext>().Database.MigrateAsync();
    }
}
