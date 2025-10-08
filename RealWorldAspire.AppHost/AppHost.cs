var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.RealWorldAspire_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.Build().Run();