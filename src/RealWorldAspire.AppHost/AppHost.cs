var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(c => c.WithLifetime(ContainerLifetime.Persistent))
    ;
var postgresdb = postgres
    .WithDataVolume("postgres-data-volume")
    // .WithDataBindMount(source: @"C:\Temp\Data", isReadOnly: false)
    // .WithEnvironment("POSTGRES_PASSWORD", "mypassword")
    .AddDatabase("realworlddb")
    ;

var apiService = builder.AddProject<Projects.RealWorldAspire_ApiService>("apiservice")
    .WithReference(postgresdb)
    // .WithEnvironment("ASPNETCORE_URLS", "http://localhost:5030")
    .WithHttpHealthCheck("/health");

builder.Build().Run();