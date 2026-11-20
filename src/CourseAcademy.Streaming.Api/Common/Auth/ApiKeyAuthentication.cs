using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using CourseAcademy.Streaming.Api.Common.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CourseAcademy.Streaming.Api.Common.Auth;

/// <summary>
/// Signs in the Main App when its request carries one of the configured keys in the X-Api-Key header.
/// Every endpoint needs this unless it is marked anonymous (the health check).
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IOptions<AuthSettings> settings) : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";
    public const string MainAppName = "main-app";
    /// <summary>Which configured key was used (1 or 2), so logs show when the old key is no longer in use.</summary>
    public const string KeyNumberClaim = "api_key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var values) || values.Count != 1 || string.IsNullOrEmpty(values[0]))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var index = ApiKeyMatcher.IndexOf(settings.Value.ApiKeys, values[0]!);
        if (index < 0)
        {
            Logger.LogWarning("Refused a request to {Path} with a wrong API key.", Request.Path);
            return Task.FromResult(AuthenticateResult.Fail("The API key is not valid."));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, MainAppName), new Claim(KeyNumberClaim, (index + 1).ToString(CultureInfo.InvariantCulture))],
            SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}

public static class ApiKeyMatcher
{
    /// <summary>
    /// The position of the configured key equal to <paramref name="candidate"/>, or -1. Compares SHA-256 hashes in
    /// constant time and always checks every key, so timing reveals neither the key nor which one matched.
    /// </summary>
    public static int IndexOf(IReadOnlyList<string> keys, string candidate)
    {
        var candidateHash = SHA256.HashData(Encoding.UTF8.GetBytes(candidate));
        var found = -1;
        for (var i = 0; i < keys.Count; i++)
        {
            var matches = CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(keys[i])), candidateHash);
            if (matches && found < 0) found = i;
        }
        return found;
    }
}
