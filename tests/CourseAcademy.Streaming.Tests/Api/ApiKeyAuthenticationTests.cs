using System.Net;
using System.Net.Http.Json;
using CourseAcademy.Streaming.Api.Common;
using CourseAcademy.Streaming.Api.Common.Auth;
using CourseAcademy.Streaming.Api.Dtos;

namespace CourseAcademy.Streaming.Tests.Api;

[Collection(ApiHostCollection.Name)]
public class ApiKeyAuthenticationTests : IClassFixture<StreamingApiFactory>
{
    private readonly StreamingApiFactory factory;

    public ApiKeyAuthenticationTests(StreamingApiFactory factory) => this.factory = factory;

    private async Task<HttpResponseMessage> PingAsync(string? key)
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/internal/v1/ping");
        if (key is not null) request.Headers.Add(ApiKeyAuthenticationHandler.HeaderName, key);
        return await client.SendAsync(request);
    }

    [Theory]
    [InlineData(StreamingApiFactory.FirstKey)]
    [InlineData(StreamingApiFactory.SecondKey)]
    public async Task Either_configured_key_is_accepted(string key)
    {
        var response = await PingAsync(key);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ping = await response.Content.ReadFromJsonAsync<PingDto>();
        Assert.Equal(ServiceInfo.Name, ping!.Service);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong-key-0123456789abcdef0123456789abc")]
    [InlineData(StreamingApiFactory.FirstKey + "x")]
    public async Task A_missing_or_wrong_key_is_refused_with_the_error_body(string? key)
    {
        var response = await PingAsync(key);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(ErrorCodes.Unauthorized, body!.Error.Code);
    }

    [Fact]
    public async Task The_health_check_needs_no_key()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var health = await response.Content.ReadFromJsonAsync<HealthDto>();
        Assert.Equal("Healthy", health!.Status);
        Assert.Equal("Healthy", health.Checks["database"]);
    }

    [Fact]
    public async Task An_unknown_route_gets_the_error_body()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationHandler.HeaderName, StreamingApiFactory.FirstKey);

        var response = await client.GetAsync("/internal/v1/nothing-here");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(ErrorCodes.NotFound, body!.Error.Code);
    }

    [Fact]
    public void The_matcher_reports_which_key_matched()
    {
        string[] keys = [StreamingApiFactory.FirstKey, StreamingApiFactory.SecondKey];

        Assert.Equal(0, ApiKeyMatcher.IndexOf(keys, StreamingApiFactory.FirstKey));
        Assert.Equal(1, ApiKeyMatcher.IndexOf(keys, StreamingApiFactory.SecondKey));
        Assert.Equal(-1, ApiKeyMatcher.IndexOf(keys, "something-else"));
    }
}
