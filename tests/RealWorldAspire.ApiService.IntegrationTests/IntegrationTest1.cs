using Microsoft.Extensions.Logging;

namespace RealWorldAspire.ApiService.IntegrationTests;

public class IntegrationTest1
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(120);

    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource(DefaultTimeout).Token;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.RealWorldAspire_AppHost>(cancellationToken);
        // appHost.Services.AddLogging(logging =>
        // {
        //     logging.SetMinimumLevel(LogLevel.Debug);
        //     // Override the logging filters from the app's configuration
        //     logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
        //     logging.AddFilter("Aspire.", LogLevel.Debug);
        //     // To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging
        // });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
    
        await using var app = await appHost.BuildAsync(cancellationToken)/*.WaitAsync(DefaultTimeout, cancellationToken)*/;
        await app.StartAsync(cancellationToken)/*.WaitAsync(DefaultTimeout, cancellationToken)*/;
    
        // Act
        var httpClient = app.CreateHttpClient("apiservice");
        // Manually check if health endpoint is accessible
        try
        {
            var response2 = await httpClient.GetAsync("/health");
            var content = await response2.Content.ReadAsStringAsync();
            Console.WriteLine($"Health check response: {response2.StatusCode} - {content}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Health check failed: {ex.Message}");
        }
        
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken)/*.WaitAsync(DefaultTimeout, cancellationToken)*/;
        var response = await httpClient.GetAsync("/", cancellationToken);
    
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
