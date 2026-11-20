using System.ComponentModel.DataAnnotations;

namespace CourseAcademy.Streaming.Api.Common.Options;

/// <summary>
/// Section "Auth". The keys are secrets: set them with user secrets or environment variables (Auth__ApiKeys__0, Auth__ApiKeys__1).
/// </summary>
public sealed class AuthSettings : IValidatableObject
{
    public const string SectionName = "Auth";
    public const int MinKeyLength = 32;
    public const int MaxKeys = 2;

    /// <summary>
    /// Keys the Main App may send in the X-Api-Key header. Two can be valid at once so a key can be changed without
    /// downtime: add the new key here, switch the Main App to it, then remove the old one.
    /// </summary>
    public string[] ApiKeys { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ApiKeys.Length is 0 or > MaxKeys)
        {
            yield return new ValidationResult($"Auth:ApiKeys must hold one or {MaxKeys} keys.", [nameof(ApiKeys)]);
            yield break;
        }
        if (ApiKeys.Any(key => key is null || key.Length < MinKeyLength || key.Trim() != key))
        {
            yield return new ValidationResult($"Every Auth:ApiKeys key must be {MinKeyLength}+ characters without spaces at the ends.", [nameof(ApiKeys)]);
        }
        if (ApiKeys.Distinct(StringComparer.Ordinal).Count() != ApiKeys.Length)
        {
            yield return new ValidationResult("Auth:ApiKeys must not hold the same key twice.", [nameof(ApiKeys)]);
        }
    }
}
