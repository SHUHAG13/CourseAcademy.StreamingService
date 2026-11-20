using System.ComponentModel.DataAnnotations;
using CourseAcademy.Streaming.Api.Common.Options;
using CourseAcademy.Streaming.Core.Storage;
using Microsoft.Extensions.Options;

namespace CourseAcademy.Streaming.Tests.Api;

[Collection(ApiHostCollection.Name)]
public class SettingsValidationTests
{
    private static readonly string GoodKey = new('k', AuthSettings.MinKeyLength);
    private static readonly string GoodSecret = "whsec_" + Convert.ToBase64String(new byte[32]);

    /// <summary>Same check as ValidateDataAnnotations at start-up: attributes, then IValidatableObject.</summary>
    private static List<string> Errors(object settings)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(settings, new ValidationContext(settings), results, validateAllProperties: true);
        return results.Select(r => r.ErrorMessage!).ToList();
    }

    private static StorageSettings GoodStorage() => new()
    {
        ServiceUrl = "https://account.r2.cloudflarestorage.com", AccessKey = "a", SecretKey = "s",
        RawBucket = "course-raw", HlsBucket = "course-hls", KeysBucket = "course-keys",
    };

    [Fact]
    public void One_or_two_long_distinct_keys_are_valid()
    {
        Assert.Empty(Errors(new AuthSettings { ApiKeys = [GoodKey] }));
        Assert.Empty(Errors(new AuthSettings { ApiKeys = [GoodKey, GoodKey + "2"] }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void No_keys_or_more_than_two_are_refused(int count)
    {
        var keys = Enumerable.Range(0, count).Select(i => GoodKey + i).ToArray();
        Assert.NotEmpty(Errors(new AuthSettings { ApiKeys = keys }));
    }

    [Fact]
    public void Short_padded_or_repeated_keys_are_refused()
    {
        Assert.NotEmpty(Errors(new AuthSettings { ApiKeys = ["short"] }));
        Assert.NotEmpty(Errors(new AuthSettings { ApiKeys = [" " + GoodKey] }));
        Assert.NotEmpty(Errors(new AuthSettings { ApiKeys = [GoodKey, GoodKey] }));
    }

    [Fact]
    public void A_webhook_needs_an_address_and_a_whsec_secret()
    {
        Assert.Empty(Errors(new WebhookSettings { Url = "https://main.test/hooks", Secret = GoodSecret }));
        Assert.NotEmpty(Errors(new WebhookSettings { Url = "not a url", Secret = GoodSecret }));
        Assert.NotEmpty(Errors(new WebhookSettings { Url = "ftp://main.test/hooks", Secret = GoodSecret }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain-secret-without-prefix-0123456789")]
    [InlineData("whsec_not-base64!")]
    [InlineData("whsec_AAAA")] // 3 bytes: too short
    public void A_webhook_secret_that_is_not_24_to_64_bytes_after_whsec_is_refused(string secret)
    {
        Assert.Null(WebhookSettings.SigningKey(secret));
        Assert.NotEmpty(Errors(new WebhookSettings { Url = "https://main.test/hooks", Secret = secret }));
    }

    [Fact]
    public void The_signing_key_is_the_decoded_part_after_whsec()
    {
        var bytes = Enumerable.Range(1, 24).Select(i => (byte)i).ToArray();
        Assert.Equal(bytes, WebhookSettings.SigningKey("whsec_" + Convert.ToBase64String(bytes)));
    }

    [Fact]
    public void A_webhook_retry_window_outside_1_to_72_hours_is_refused()
    {
        Assert.NotEmpty(Errors(new WebhookSettings { Url = "https://main.test/hooks", Secret = GoodSecret, RetryHours = 0 }));
        Assert.NotEmpty(Errors(new WebhookSettings { Url = "https://main.test/hooks", Secret = GoodSecret, RetryHours = 73 }));
    }

    [Fact]
    public void Complete_storage_settings_are_valid()
    {
        Assert.Empty(Errors(GoodStorage()));
    }

    [Theory]
    [InlineData("Course-Raw")]
    [InlineData("ab")]
    [InlineData("-raw")]
    [InlineData("raw_files")]
    public void A_bucket_name_storage_would_refuse_is_refused(string bucket)
    {
        var storage = GoodStorage();
        storage.RawBucket = bucket;
        Assert.NotEmpty(Errors(storage));
    }

    [Fact]
    public void The_three_buckets_must_differ()
    {
        var storage = GoodStorage();
        storage.KeysBucket = storage.HlsBucket;
        Assert.Contains(Errors(storage), e => e.Contains("three different buckets"));
    }

    [Fact]
    public void Storage_without_address_or_credentials_is_refused()
    {
        Assert.Equal(3, Errors(new StorageSettings
        {
            RawBucket = "course-raw", HlsBucket = "course-hls", KeysBucket = "course-keys",
        }).Count);
    }

    [Fact]
    public void The_api_does_not_start_with_plain_http_outside_development()
    {
        using var factory = new StreamingApiFactory().With("Webhook:Url", "http://main.test/hooks");

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("Webhook:Url must use https", exception.Message);
    }

    [Fact]
    public void The_api_does_not_start_without_api_keys()
    {
        using var factory = new StreamingApiFactory().With("Auth:ApiKeys:0", null).With("Auth:ApiKeys:1", null);

        Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
    }
}
