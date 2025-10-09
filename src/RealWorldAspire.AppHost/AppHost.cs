var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.RealWorldAspire_ApiService>("apiservice")
    // .WithEnvironment("ASPNETCORE_URLS", "http://localhost:5030")
    .WithHttpHealthCheck("/health");

builder.Build().Run();