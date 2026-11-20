using System.Reflection;
using CourseAcademy.Streaming.Api.Common.Auth;
using CourseAcademy.Streaming.Api.Common.Options;
using CourseAcademy.Streaming.Api.Data;
using CourseAcademy.Streaming.Core.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

namespace CourseAcademy.Streaming.Api.Common;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStreamingApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddOptions<DatabaseSettings>().Bind(configuration.GetSection(DatabaseSettings.SectionName));
        services.AddOptions<StorageSettings>().Bind(configuration.GetSection(StorageSettings.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<WebhookSettings>().Bind(configuration.GetSection(WebhookSettings.SectionName)).ValidateDataAnnotations().ValidateOnStart();

        services.AddDatabase(configuration);
        services.AddAuth(configuration);
        services.AddWebApi();
        return services;
    }

    private static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing. Set ConnectionStrings__DefaultConnection in the environment or user secrets.");
        }

        services.AddSingleton<AuditInterceptor>();
        services.AddDbContext<StreamingDbContext>((sp, options) => options
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null))
            .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));
    }

    /// <summary>Every endpoint needs the Main App's API key unless it is explicitly anonymous.</summary>
    private static void AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuthSettings>().Bind(configuration.GetSection(AuthSettings.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, null);
        services.AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder(ApiKeyAuthenticationHandler.SchemeName)
            .RequireAuthenticatedUser()
            .Build());
    }

    private static void AddWebApi(this IServiceCollection services)
    {
        services
            .AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                // Model-binding errors (wrong types, unreadable JSON) use the same error body as everything else.
                options.InvalidModelStateResponseFactory = context =>
                {
                    var fields = context.ModelState
                        .Where(entry => entry.Value?.Errors.Count > 0)
                        .SelectMany(entry => entry.Value!.Errors.Select(error => new FieldError(
                            ToCamelCasePath(entry.Key.TrimStart('$', '.')),
                            string.IsNullOrWhiteSpace(error.ErrorMessage) ? "This value is not valid." : error.ErrorMessage)))
                        .ToList();
                    return new BadRequestObjectResult(ErrorResponse.Of(ErrorCodes.ValidationFailed, "The request has invalid fields.", fields));
                };
            });

        services.AddHealthChecks().AddDbContextCheck<StreamingDbContext>("database");

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new() { Title = "Course Academy Streaming API", Version = "v1" });
            var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            if (File.Exists(xml)) options.IncludeXmlComments(xml);
            // Lets Scalar and Swagger UI send the key as "X-Api-Key: <key>".
            options.AddSecurityDefinition(ApiKeyAuthenticationHandler.SchemeName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = ApiKeyAuthenticationHandler.HeaderName,
                Description = "One of the keys in Auth:ApiKeys.",
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(ApiKeyAuthenticationHandler.SchemeName, document)] = [],
            });
        });
    }

    /// <summary>"Parts[0].ETag" → "parts[0].eTag": field paths in errors use the JSON (camelCase) names.</summary>
    internal static string ToCamelCasePath(string path) =>
        string.Join('.', path.Split('.').Select(part => part.Length == 0 ? part : char.ToLowerInvariant(part[0]) + part[1..]));
}
