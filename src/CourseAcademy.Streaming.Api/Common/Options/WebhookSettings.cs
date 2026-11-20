using System.ComponentModel.DataAnnotations;

namespace CourseAcademy.Streaming.Api.Common.Options;

/// <summary>
/// Section "Webhook": where events are sent (the Main App) and how they are signed (Standard Webhooks).
/// Secret is a secret: set it with user secrets or the environment variable Webhook__Secret.
/// </summary>
public sealed class WebhookSettings : IValidatableObject
{
    public const string SectionName = "Webhook";
    public const string SecretPrefix = "whsec_";
    public const int MinSecretBytes = 24;
    public const int MaxSecretBytes = 64;

    /// <summary>The Main App's webhook endpoint, e.g. https://api.example.com/api/v1/internal/streaming/webhooks.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>"whsec_" followed by 24–64 random bytes in base64. The Main App holds the same value.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>How long an event that the Main App does not accept is retried before it is given up.</summary>
    [Range(1, 72)]
    public int RetryHours { get; set; } = 24;

    /// <summary>The signing key inside a "whsec_…" secret, or null when the secret is not in that form.</summary>
    public static byte[]? SigningKey(string secret)
    {
        if (!secret.StartsWith(SecretPrefix, StringComparison.Ordinal)) return null;
        var encoded = secret[SecretPrefix.Length..];
        var buffer = new byte[encoded.Length];
        if (!Convert.TryFromBase64String(encoded, buffer, out var length)) return null;
        return length is >= MinSecretBytes and <= MaxSecretBytes ? buffer[..length] : null;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            yield return new ValidationResult("Webhook:Url must be the Main App's http(s) webhook address.", [nameof(Url)]);
        }
        if (SigningKey(Secret) is null)
        {
            yield return new ValidationResult(
                $"Webhook:Secret must be \"{SecretPrefix}\" followed by {MinSecretBytes}–{MaxSecretBytes} random bytes in base64.", [nameof(Secret)]);
        }
    }
}
