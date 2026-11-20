using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CourseAcademy.Streaming.Core.Storage;

/// <summary>
/// Section "Storage": the S3-compatible object storage (Cloudflare R2 in production, MinIO in development).
/// AccessKey and SecretKey are secrets: set them with user secrets or environment variables (Storage__AccessKey, Storage__SecretKey).
/// </summary>
public sealed partial class StorageSettings : IValidatableObject
{
    public const string SectionName = "Storage";

    /// <summary>S3 endpoint, e.g. https://&lt;account&gt;.r2.cloudflarestorage.com or http://localhost:9000.</summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>R2 uses "auto"; MinIO accepts any region (us-east-1 by default).</summary>
    public string Region { get; set; } = "auto";

    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Path-style URLs (http://host/bucket/key). MinIO needs this; R2 works either way.</summary>
    public bool ForcePathStyle { get; set; }

    /// <summary>Uploaded originals (videos and documents). Originals are kept; deleted ones move under archive/.</summary>
    public string RawBucket { get; set; } = string.Empty;

    /// <summary>HLS output (playlists, segments, subtitles) and thumbnails. Read by the playback gateway.</summary>
    public string HlsBucket { get; set; } = string.Empty;

    /// <summary>AES-128 keys. Never public; only the playback gateway reads it after checking the token.</summary>
    public string KeysBucket { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Uri.TryCreate(ServiceUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            yield return new ValidationResult("Storage:ServiceUrl must be the storage's http(s) address.", [nameof(ServiceUrl)]);
        }
        if (string.IsNullOrWhiteSpace(Region)) yield return new ValidationResult("Storage:Region must be set.", [nameof(Region)]);
        if (string.IsNullOrWhiteSpace(AccessKey)) yield return new ValidationResult("Storage:AccessKey must be set.", [nameof(AccessKey)]);
        if (string.IsNullOrWhiteSpace(SecretKey)) yield return new ValidationResult("Storage:SecretKey must be set.", [nameof(SecretKey)]);

        (string Name, string Value)[] buckets = [(nameof(RawBucket), RawBucket), (nameof(HlsBucket), HlsBucket), (nameof(KeysBucket), KeysBucket)];
        foreach (var (name, value) in buckets.Where(b => !BucketName().IsMatch(b.Value)))
        {
            yield return new ValidationResult($"Storage:{name} must be a bucket name: 3–63 lowercase letters, digits or hyphens.", [name]);
        }
        // The keys must never sit next to the public HLS files, and archived originals must not mix with either.
        if (buckets.Select(b => b.Value).Distinct(StringComparer.OrdinalIgnoreCase).Count() != buckets.Length)
        {
            yield return new ValidationResult("Storage:RawBucket, HlsBucket and KeysBucket must be three different buckets.", [nameof(RawBucket)]);
        }
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$")]
    private static partial Regex BucketName();
}
