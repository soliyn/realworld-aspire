using RealWorldAspire.ApiService.IntegrationTests.TestUtilities;
using Shouldly;

namespace RealWorldAspire.ApiService.IntegrationTests.Features;

[Collection(nameof(AspireTestCollection))]
public class EndpointsTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private readonly AspireTestFixture _fixture;

    public EndpointsTests(AspireTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task RequireAuthentication()
    {
        var cancellationToken = new CancellationTokenSource(DefaultTimeout).Token;
        using var httpClient = _fixture.App.CreateHttpClient("apiservice");

        // User endpoints
        (await httpClient.GetAsync("/api/user", cancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await httpClient.PutAsync("/api/user", null, cancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // Profile endpoints
        (await httpClient.PostAsync("/api/profiles/someuser/follow", null, cancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await httpClient.DeleteAsync("/api/profiles/someuser/follow", cancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // Article endpoints
        (await httpClient.GetAsync("/api/articles/feed", cancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await httpClient.PostAsync("/api/articles", null, cancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await httpClient.PutAsync("/api/articles/some-slug", null, cancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await httpClient.DeleteAsync("/api/articles/some-slug", cancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // Comment endpoints
        (await httpClient.PostAsync("/api/articles/some-slug/comments", null, cancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await httpClient.DeleteAsync("/api/articles/some-slug/comments/1", cancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // Favorite endpoints
        (await httpClient.PostAsync("/api/articles/some-slug/favorite", null, cancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await httpClient.DeleteAsync("/api/articles/some-slug/favorite", cancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    } 
}